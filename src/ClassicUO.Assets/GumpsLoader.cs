﻿#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.IO;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public class GumpsLoader : UOFileLoader
    {
        private static GumpsLoader _instance;
        private UOFile _file;
        public PixelPicker _picker;

        public GumpsLoader(PixelPicker picker)
        {
            _picker = picker;
        }

        public const int MAX_GUMP_DATA_INDEX_COUNT = 0x10000;

        private GumpsLoader(int count) { }

        public static GumpsLoader Instance =>
            _instance ?? (_instance = new GumpsLoader(MAX_GUMP_DATA_INDEX_COUNT));

        public bool UseUOPGumps = false;

        public override Task Load()
        {
            return Task.Run(() =>
            {
                string path = UOFileManager.GetUOFilePath("gumpartLegacyMUL.uop");

                if (UOFileManager.IsUOPInstallation && File.Exists(path))
                {
                    _file = new UOFileUop(path, "build/gumpartlegacymul/{0:D8}.tga", true);
                    Entries = new UOFileIndex[
                        Math.Max(((UOFileUop)_file).TotalEntriesCount, MAX_GUMP_DATA_INDEX_COUNT)
                    ];
                    UseUOPGumps = true;
                }
                else
                {
                    path = UOFileManager.GetUOFilePath("gumpart.mul");
                    string pathidx = UOFileManager.GetUOFilePath("gumpidx.mul");

                    if (!File.Exists(path))
                    {
                        path = UOFileManager.GetUOFilePath("Gumpart.mul");
                    }

                    if (!File.Exists(pathidx))
                    {
                        pathidx = UOFileManager.GetUOFilePath("Gumpidx.mul");
                    }

                    _file = new UOFileMul(path, pathidx, MAX_GUMP_DATA_INDEX_COUNT, 12);

                    UseUOPGumps = false;
                }

                _file.FillEntries(ref Entries);

                string pathdef = UOFileManager.GetUOFilePath("gump.def");

                if (!File.Exists(pathdef))
                {
                    return;
                }

                using (DefReader defReader = new DefReader(pathdef, 3))
                {
                    while (defReader.Next())
                    {
                        int ingump = defReader.ReadInt();

                        if (
                            ingump < 0
                            || ingump >= MAX_GUMP_DATA_INDEX_COUNT
                            || ingump >= Entries.Length
                            || Entries[ingump].Length > 0
                        )
                        {
                            continue;
                        }

                        int[] group = defReader.ReadGroup();

                        if (group == null)
                        {
                            continue;
                        }

                        for (int i = 0; i < group.Length; i++)
                        {
                            int checkIndex = group[i];

                            if (
                                checkIndex < 0
                                || checkIndex >= MAX_GUMP_DATA_INDEX_COUNT
                                || checkIndex >= Entries.Length
                                || Entries[checkIndex].Length <= 0
                            )
                            {
                                continue;
                            }

                            Entries[ingump] = Entries[checkIndex];

                            Entries[ingump].Hue = (ushort)defReader.ReadInt();

                            break;
                        }
                    }
                }
            });
        }

        const int ATLAS_SIZE = 1024 * 4;
        private TextureAtlas _atlas;

        public void CreateAtlas(GraphicsDevice device)
        {
            _atlas = new TextureAtlas(device, ATLAS_SIZE, ATLAS_SIZE, SurfaceFormat.Color);
        }

        struct SpriteInfo
        {
            public Texture2D Texture;
            public Rectangle UV;
        }

        private SpriteInfo[] _spriteInfos;

        public Texture2D GetGumpTexture(ushort graphic, out Rectangle bounds)
        {
            PNGLoader pngLoader = new PNGLoader(/* parâmetros do construtor, se houver */);

            // ## BEGIN - END ## // TAZUO
            Texture2D png = pngLoader.LoadGumpTexture2d(graphic);
            if (png != null)
            {
                bounds = png.Bounds;
                return png;
            }
            // ## BEGIN - END ## // TAZUO

            ref var spriteInfo = ref _spriteInfos[graphic];

            if (spriteInfo.Texture == null)
            {
                AddSpriteToAtlas(_atlas, graphic);
            }

            bounds = spriteInfo.UV;

            return spriteInfo.Texture;
        }

        private unsafe void AddSpriteToAtlas(TextureAtlas atlas, uint index)
        {
            ref UOFileIndex entry = ref GetValidRefEntry((int)index);

            if (entry.Width <= 0 && entry.Height <= 0)
            {
                return;
            }

            ushort color = entry.Hue;

            _file.SetData(entry.Address, entry.FileSize);
            _file.Seek(entry.Offset);

            IntPtr dataStart = _file.PositionAddress;

            uint[] buffer = null;

            Span<uint> pixels = entry.Width * entry.Height <= 1024 ? stackalloc uint[1024] : (buffer = System.Buffers.ArrayPool<uint>.Shared.Rent(entry.Width * entry.Height));

            try
            {
                int* lookuplist = (int*)dataStart;

                int gsize;

                for (int y = 0, half_len = entry.Length >> 2; y < entry.Height; y++)
                {
                    if (y < entry.Height - 1)
                    {
                        gsize = lookuplist[y + 1] - lookuplist[y];
                    }
                    else
                    {
                        gsize = half_len - lookuplist[y];
                    }

                    GumpBlock* gmul = (GumpBlock*)(dataStart + (lookuplist[y] << 2));

                    int pos = y * entry.Width;

                    for (int i = 0; i < gsize; i++)
                    {
                        uint val = gmul[i].Value;

                        if (color != 0 && val != 0)
                        {
                            val = HuesLoader.Instance.GetPartialHueColor(gmul[i].Value, color);
                        }

                        if (val != 0)
                        {
                            //val = 0x8000 | val;
                            val = HuesHelper.Color16To32(gmul[i].Value) | 0xFF_00_00_00;
                        }

                        int count = gmul[i].Run;

                        for (int j = 0; j < count; j++)
                        {
                            pixels[pos++] = val;
                        }
                    }
                }

                ref var spriteInfo = ref _spriteInfos[index];


                Microsoft.Xna.Framework.Rectangle uvRectangle;
                spriteInfo.Texture = atlas.AddSprite(pixels, entry.Width, entry.Height, out uvRectangle);
                spriteInfo.UV = new Microsoft.Xna.Framework.Rectangle(uvRectangle.X, uvRectangle.Y, uvRectangle.Width, uvRectangle.Height);
                _picker.Set(index, entry.Width, entry.Height, pixels);
            }
            finally
            {
                if (buffer != null)
                {
                    System.Buffers.ArrayPool<uint>.Shared.Return(buffer, true);
                }
            }
        }

        public unsafe GumpInfo GetGump(uint index)
        {
            ref UOFileIndex entry = ref GetValidRefEntry((int)index);

            if (entry.Width <= 0 && entry.Height <= 0)
            {
                return default;
            }

            ushort color = entry.Hue;

            _file.SetData(entry.Address, entry.FileSize);
            _file.Seek(entry.Offset);


            ReadOnlySpan<byte> output;
            var newFileFormat = UOFileManager.Version >= ClientVersion.CV_7010400;
            if (newFileFormat)
            {
                var cbuf = _file.ReadArray(entry.Length);
                var dbuf = new byte[entry.DecompressedLength];
                var result = ZLib.Decompress(cbuf, 0, dbuf, dbuf.Length);
                if (result != ZLib.ZLibError.Okay)
                {
                    return default;
                }

                output = BwtDecompress.Decompress(dbuf);
            }
            else
            {
                output = new ReadOnlySpan<byte>(_file.PositionAddress.ToPointer(), entry.Length);
            }

            var reader = new StackDataReader(output);
            var w = newFileFormat ? reader.ReadUInt32LE() : (uint)entry.Width;
            var h = newFileFormat ? reader.ReadUInt32LE() : (uint)entry.Height;

            IntPtr dataStart = reader.PositionAddress;
            var pixels = new uint[w * h];
            int* lookuplist = (int*)dataStart;
            int gsize;
            var len = reader.Remaining;

            for (int y = 0, half_len = len >> 2; y < h; y++)
            {
                if (y < h - 1)
                {
                    gsize = lookuplist[y + 1] - lookuplist[y];
                }
                else
                {
                    gsize = half_len - lookuplist[y];
                }

                GumpBlock* gmul = (GumpBlock*)(dataStart + (lookuplist[y] << 2));

                var pos = y * w;

                for (int i = 0; i < gsize; i++)
                {
                    uint val = gmul[i].Value;

                    if (color != 0 && val != 0)
                    {
                        val = HuesLoader.Instance.ApplyHueRgba5551(gmul[i].Value, color);
                    }

                    if (val != 0)
                    {
                        //val = 0x8000 | val;
                        val = HuesHelper.Color16To32(gmul[i].Value) | 0xFF_00_00_00;
                    }

                    var count = gmul[i].Run;
                    pixels.AsSpan().Slice((int)pos, count).Fill(val);
                    pos += count;
                }
            }

            return new GumpInfo()
            {
                Pixels = pixels,
                Width = (int)w,
                Height = (int)h
            };
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private ref struct GumpBlock
        {
            public readonly ushort Value;
            public readonly ushort Run;
        }
    }

    public ref struct GumpInfo
    {
        public Span<uint> Pixels;
        public int Width;
        public int Height;
    }
}
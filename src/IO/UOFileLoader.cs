﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassicUO.Game;
using ClassicUO.Renderer;

namespace ClassicUO.IO
{
    internal abstract class UOFileLoader : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;

            ClearResources();
        }

        public UOFileIndex[] Entries;

        public abstract Task Load();

        public virtual void ClearResources()
        {
        }

        public ref UOFileIndex GetValidRefEntry(int index)
        {
            if (index < 0 || Entries == null || index >= Entries.Length)
            {
                return ref UOFileIndex.Invalid;
            }

            ref UOFileIndex entry = ref Entries[index];

            if (entry.Offset < 0 || entry.Length <= 0 || entry.Offset == 0x0000_0000_FFFF_FFFF)
            {
                return ref UOFileIndex.Invalid;
            }

            return ref entry;
        }
    }

    internal abstract class UOFileLoader<T> : UOFileLoader where T : UOTexture
    {
        private readonly LinkedList<uint> _usedTextures = new LinkedList<uint>();

        protected UOFileLoader(int max)
        {
            Resources = new T[max];
        }

        protected readonly T[] Resources;

        public abstract T GetTexture(uint id);

        protected void SaveId(uint id)
        {
            _usedTextures.AddLast(id);
        }

        public virtual void CleaUnusedResources(int count)
        {
            ClearUnusedResources(Resources, count);
        }

        public override void ClearResources()
        {
            LinkedListNode<uint> first = _usedTextures.First;

            while (first != null)
            {
                LinkedListNode<uint> next = first.Next;
                uint idx = first.Value;

                if (idx < Resources.Length)
                {
                    ref T texture = ref Resources[idx];
                    texture?.Dispose();
                    texture = null;
                }

                _usedTextures.Remove(first);

                first = next;
            }
        }

        public void ClearUnusedResources<T1>(T1[] resourceCache, int maxCount) where T1 : UOTexture
        {
            if (Time.Ticks <= Constants.CLEAR_TEXTURES_DELAY)
            {
                return;
            }

            long ticks = Time.Ticks - Constants.CLEAR_TEXTURES_DELAY;
            int count = 0;

            LinkedListNode<uint> first = _usedTextures.First;

            while (first != null)
            {
                LinkedListNode<uint> next = first.Next;
                uint idx = first.Value;

                if (idx < resourceCache.Length && resourceCache[idx] != null)
                {
                    if (resourceCache[idx].Ticks < ticks)
                    {
                        if (count++ >= maxCount)
                        {
                            break;
                        }

                        resourceCache[idx].Dispose();

                        resourceCache[idx] = null;
                        _usedTextures.Remove(first);
                    }
                }

                first = next;
            }
        }


        public virtual bool TryGetEntryInfo(int entry, out long address, out long size, out long compressedSize)
        {
            address = size = compressedSize = 0;

            return false;
        }
    }
}
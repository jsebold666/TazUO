#region license

// Copyright (c) 2021, andreakarasho
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

using System;
using System.Security.Cryptography;
using System.IO;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;
using ClassicUO.Utility.Logging;
using System.Text.RegularExpressions;
using System.Linq;

namespace ClassicUO.Game.Managers
{
    internal class JournalManager
    {
        private StreamWriter _fileWriter;
        private bool _writerHasException;
        private static SHA1 hash = new SHA1CryptoServiceProvider();

        public static Deque<JournalEntry> Entries { get; } = new Deque<JournalEntry>(Constants.MAX_JOURNAL_HISTORY_COUNT);

        public event EventHandler<JournalEntry> EntryAdded;

        public void Add(string text, ushort hue, string name, TextType type, bool isunicode = true, MessageType messageType = MessageType.Regular, DateTime? time = null)
        {
            JournalEntry entry = Entries.Count >= Constants.MAX_JOURNAL_HISTORY_COUNT ? Entries.RemoveFromFront() : new JournalEntry();

            byte font = (byte) (isunicode ? 0 : 9);

            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.OverrideAllFonts)
            {
                font = ProfileManager.CurrentProfile.ChatFont;
                isunicode = ProfileManager.CurrentProfile.OverrideAllFontsIsUnicode;
            }

            DateTime timeNow = DateTime.Now;
            if (time.HasValue)
            {
                timeNow = time.Value;
            }

            entry.Text = text;
            entry.Font = font;
            entry.Hue = hue;
            entry.Name = name;
            entry.IsUnicode = isunicode;
            entry.Time = timeNow;
            entry.TextType = type;
            entry.MessageType = messageType;

            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ForceUnicodeJournal)
            {
                entry.Font = 0;
                entry.IsUnicode = true;
            }

            Entries.AddToBack(entry);
            EntryAdded.Raise(entry);

            if (_fileWriter == null && !_writerHasException)
            {
                CreateWriter();
            }

            _fileWriter?.WriteLine($"[{timeNow:g}]  {name}: {text}");
        }
        public static uint GetUInt32HashCode(JournalEntry entry)
        {
            if (entry == null)
            {
                return 0;
            }
            return GetUInt32HashCode($"{entry.TextType}|{entry.MessageType}[{entry.Time:g}]  {entry.Name}: {entry.Text}");
        }
        /// <summary>
        /// Generate a int signature based on the provided string input.
        /// </summary>
        /// <param name="strText"></param>
        /// <returns></returns>
        public static uint GetUInt32HashCode(string strText)
        {
            if (string.IsNullOrEmpty(strText)) return 0;

            //Unicode Encode Covering all characterset
            byte[] byteContents = System.Text.Encoding.Unicode.GetBytes(strText);
            byte[] hashText = hash.ComputeHash(byteContents);
            uint hashCodeStart = BitConverter.ToUInt32(hashText, 0);
            uint hashCodeMedium = BitConverter.ToUInt32(hashText, 8);
            uint hashCodeEnd = BitConverter.ToUInt32(hashText, 16);
            var hashCode = hashCodeStart ^ hashCodeMedium ^ hashCodeEnd;
            return uint.MaxValue - hashCode;
        }
        /// <summary>
        /// UOAlive > Add Global Chat To Journal
        /// </summary>
        /// <param name="lines"></param>
        public void ProcessGlobalChatLines(string[] lines)
        {
            // Each GlobalChat Request contains all lines of chat.
            // Here we are breaking into chunks based on <BR>
            var lstLine = lines[0].Split(new[] { "<BR>" }, StringSplitOptions.RemoveEmptyEntries).Reverse().ToArray();
            foreach (var line in lstLine)
            {
                // Stripping out the HTML formating of <BASEFONTCOLOR
                // Left with following format [hh:mm] [Name]: [Text]
                var text = Regex.Replace(line, "<[^>]*>", string.Empty).Trim();

                var parts = text.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    //attempt to parse the hour/minute from text
                    var timeNameSpilt = parts[0].Split(' ');
                    var ts = TimeSpan.Parse(timeNameSpilt[0].Replace("[", string.Empty).Replace("]", string.Empty));

                    var name = timeNameSpilt[1];
                    var message = $"{name}: {parts[1]}";

                    // add timestamp to utcdate..Server sends time relative to utc
                    var time = DateTime.UtcNow.Date.Add(ts);
                    // attempt to determine client time vs servier time offset.
                    var offSet = (DateTime.UtcNow.Hour - DateTime.Now.Hour) * -1;

                    //adjust for client time offset.
                    time = time.AddHours(offSet);

                    Add(message, 0x0973, "Global", TextType.SYSTEM, messageType: MessageType.Global, time: time);
                }

            }
        }

        private void CreateWriter()
        {
            if (_fileWriter == null && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.SaveJournalToFile)
            {
                try
                {
                    string path = FileSystemHelper.CreateFolderIfNotExists(Path.Combine(CUOEnviroment.ExecutablePath, "Data"), "Client", "JournalLogs");

                    _fileWriter = new StreamWriter(File.Open(Path.Combine(path, $"{DateTime.Now:yyyy_MM_dd_HH_mm_ss}_journal.txt"), FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        AutoFlush = true
                    };

                    try
                    {
                        string[] files = Directory.GetFiles(path, "*_journal.txt");
                        Array.Sort(files);

                        for (int i = files.Length - 1; i >= 100; --i)
                        {
                            File.Delete(files[i]);
                        }
                    }
                    catch
                    {
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    // we don't want to wast time.
                    _writerHasException = true;
                }
            }
        }

        public void CloseWriter()
        {
            _fileWriter?.Flush();
            _fileWriter?.Dispose();
            _fileWriter = null;
        }

        public void Clear()
        {
            //Entries.Clear();
            CloseWriter();
        }
    }

    internal class JournalEntry
    {
        public byte Font;
        public ushort Hue;

        public bool IsUnicode;
        public string Name;
        public string Text;

        public TextType TextType;
        public DateTime Time;
        public MessageType MessageType;
    }
}
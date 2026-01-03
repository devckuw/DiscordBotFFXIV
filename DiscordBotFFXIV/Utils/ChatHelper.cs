using DiscordBotFFXIV;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DiscordBotFFXIV.Utils
{
    public enum ChatMode
    {
        None = 0,
        Echo = 1,
        Party = 2,
        Alliance = 3,
        Say = 4,
        Shout = 5,
        Yell = 6,
        FreeCompany = 7,
        LinkShell1 = 8,
        LinkShell2 = 9,
        LinkShell3 = 10,
        LinkShell4 = 11,
        LinkShell5 = 12,
        LinkShell6 = 13,
        LinkShell7 = 14,
        LinkShell8 = 15,
        Emote = 16,
        CrossLinkShell1 = 17,
        CrossLinkShell2 = 18,
        CrossLinkShell3 = 19,
        CrossLinkShell4 = 20,
        CrossLinkShell5 = 21,
        CrossLinkShell6 = 22,
        CrossLinkShell7 = 23,
        CrossLinkShell8 = 24,
        Tell = 25,
        e = 26,
        p = 27,
        a = 28,
        s = 29,
        sh = 30,
        y = 31,
        fc = 32,
        l1 = 33,
        l2 = 34,
        l3 = 35,
        l4 = 36,
        l5 = 37,
        l6 = 38,
        l7 = 39,
        l8 = 40,
        em = 41,
        cwl1 = 42,
        cwl2 = 43,
        cwl3 = 44,
        cwl4 = 45,
        cwl5 = 46,
        cwl6 = 47,
        cwl7 = 48,
        cwl8 = 49,
        t = 50
    }

    internal unsafe class ChatHelper : IDisposable
    {
        #region Singleton
        private ChatHelper()
        {
            if (Plugin.SigScanner.TryScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B F2 48 8B F9 45 84 C9", out IntPtr ptr))
            {
                _chatModulePtr = ptr;
            }
        }

        public static void Initialize() { Instance = new ChatHelper(); }

        public static ChatHelper Instance { get; private set; } = null!;

        ~ChatHelper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Instance = null!;
        }
        #endregion

        private IntPtr _chatModulePtr;

        public static unsafe bool IsInputTextActive()
        {
            Framework* framework = Framework.Instance();
            if (framework == null) { return false; }

            UIModule* module = framework->GetUIModule();
            if (module == null) { return false; }

            RaptureAtkModule* atkModule = module->GetRaptureAtkModule();
            if (atkModule == null) { return false; }

            return atkModule->AtkModule.IsTextInputActive();
        }

        public static void Send(ChatMode mode, string msg)
        {
            if (Instance == null)
            {
                return;
            }
            if (mode == ChatMode.None)
            {
                SendChatMessage("/e No Chat mode selected.");
                return;
            }
            msg = "/" + mode.ToString().ToLower() + " " + msg;
            SendChatMessage(msg);
        }

        public static void SendChatMessage(string message)
        {
            if (Instance == null)
            {
                return;
            }

            Instance.SendMessage(message);
        }

        private void SendMessage(string message)
        {
            if (message == null || message.Length == 0)
            {
                return;
            }

            // let dalamud process the command first
            if (Plugin.CommandManager.ProcessCommand(message))
            {
                return;
            }

            if (_chatModulePtr == IntPtr.Zero)
            {
                return;
            }

            // encode message
            var (text, length) = EncodeMessage(message);
            var payload = MessagePayload(text, length);

            ChatDelegate chatDelegate = Marshal.GetDelegateForFunctionPointer<ChatDelegate>(_chatModulePtr);
            chatDelegate.Invoke(Plugin.GameGui.GetUIModule(), payload, IntPtr.Zero, (byte)0);

            Marshal.FreeHGlobal(payload);
            Marshal.FreeHGlobal(text);
        }

        private static (IntPtr, long) EncodeMessage(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            var mem = Marshal.AllocHGlobal(bytes.Length + 30);
            Marshal.Copy(bytes, 0, mem, bytes.Length);
            Marshal.WriteByte(mem + bytes.Length, 0);
            return (mem, bytes.Length + 1);
        }

        private static IntPtr MessagePayload(IntPtr message, long length)
        {
            var mem = Marshal.AllocHGlobal(400);
            Marshal.WriteInt64(mem, message.ToInt64());
            Marshal.WriteInt64(mem + 0x8, 64);
            Marshal.WriteInt64(mem + 0x10, length);
            Marshal.WriteInt64(mem + 0x18, 0);
            return mem;
        }

        public static ChatMode GetChatMode(string s)
        {
            switch (s)
            {
                case "l1":
                    return ChatMode.l1;
                case "l2":
                    return ChatMode.l2;
                case "l3":
                    return ChatMode.l3;
                case "l4":
                    return ChatMode.l4;
                case "l5":
                    return ChatMode.l5;
                case "l6":
                    return ChatMode.l6;
                case "l7":
                    return ChatMode.l7;
                case "l8":
                    return ChatMode.l8;
                case "cwl1":
                    return ChatMode.cwl1;
                case "cwl2":
                    return ChatMode.cwl2;
                case "cwl3":
                    return ChatMode.cwl3;
                case "cwl4":
                    return ChatMode.cwl4;
                case "cwl5":
                    return ChatMode.cwl5;
                case "cwl6":
                    return ChatMode.cwl6;
                case "cwl7":
                    return ChatMode.cwl7;
                case "cwl8":
                    return ChatMode.cwl8;
                default:
                    return ChatMode.None;
            }
        }
    }

    public delegate IntPtr UiModuleDelegate(IntPtr baseUiPtr);
    public delegate IntPtr ChatDelegate(IntPtr uiModulePtr, IntPtr message, IntPtr unknown1, byte unknown2);
}

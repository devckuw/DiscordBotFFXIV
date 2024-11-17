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
        None,
        Echo,
        Party,
        Alliance,
        Say,
        Shout,
        Yell,
        FreeCompany,
        LinkShell1,
        LinkShell2,
        LinkShell3,
        LinkShell4,
        LinkShell5,
        LinkShell6,
        LinkShell7,
        LinkShell8,
        Emote,
        CrossLinkShell1,
        CrossLinkShell2,
        CrossLinkShell3,
        CrossLinkShell4,
        CrossLinkShell5,
        CrossLinkShell6,
        CrossLinkShell7,
        CrossLinkShell8,
        Tell,
        e,
        p,
        a,
        s,
        sh,
        y,
        fc,
        l1,
        l2,
        l3,
        l4,
        l5,
        l6,
        l7,
        l8,
        em,
        cwl1,
        cwl2,
        cwl3,
        cwl4,
        cwl5,
        cwl6,
        cwl7,
        cwl8,
        t
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
    }

    public delegate IntPtr UiModuleDelegate(IntPtr baseUiPtr);
    public delegate IntPtr ChatDelegate(IntPtr uiModulePtr, IntPtr message, IntPtr unknown1, byte unknown2);
}

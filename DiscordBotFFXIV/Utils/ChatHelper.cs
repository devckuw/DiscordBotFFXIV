using DiscordBotFFXIV;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;

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


    public static class ChatHelper
    {
        public static void Send(ChatMode mode, string msg)
        {
            if (mode == ChatMode.None)
            {
                ExecuteCommand("/e No Chat mode selected.");
                return;
            }
            msg = "/" + mode.ToString().ToLower() + " " + msg;
            ExecuteCommand(msg);
        }

        public static unsafe void ExecuteCommand(string command)
        {
            if (!command.StartsWith('/'))
                return;

            using var cmd = new Utf8String(command);

            // Technically not needed since we don't use payloads but provides a better example.
            cmd.SanitizeString(
                AllowedEntities.Unknown9     |
                AllowedEntities.Payloads          |
                AllowedEntities.OtherCharacters   |
                AllowedEntities.SpecialCharacters |
                AllowedEntities.Numbers           |
                AllowedEntities.LowercaseLetters  |
                AllowedEntities.UppercaseLetters  );

            if (cmd.Length > 500)
                return;

            RaptureShellModule.Instance()->ExecuteCommandInner(&cmd, UIModule.Instance());
        }

        public static unsafe bool IsInputTextActive => RaptureAtkModule.Instance()->IsTextInputActive();


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
}

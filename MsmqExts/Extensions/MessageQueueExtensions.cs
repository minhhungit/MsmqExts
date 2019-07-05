﻿// Inspired by https://www.hangfire.io/

using System;
using System.ComponentModel;

#if NET462
using System.Messaging;
#else
using Experimental.System.Messaging;
#endif

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MsmqExts
{
    public static class MessageQueueExtensions
    {
#region P/Invoke stuff

        [DllImport("mqrt.dll")]
        private static extern int MQMgmtGetInfo(
            [MarshalAs(UnmanagedType.BStr)]string computerName,
            [MarshalAs(UnmanagedType.BStr)]string objectName,
            ref MQMGMTPROPS mgmtProps);

        private const byte VT_NULL = 1;
        private const byte VT_UI4 = 19;
        private const int PROPID_MGMT_QUEUE_MESSAGE_COUNT = 7;

        //size must be 16
        [StructLayout(LayoutKind.Sequential)]
        private struct MQPROPVariant
        {
            public byte vt;       //0
            public byte spacer;   //1
            public short spacer2; //2
            public int spacer3;   //4
            public uint ulVal;    //8
            public int spacer4;   //12
        }

        //size must be 16 in x86 and 28 in x64
        [StructLayout(LayoutKind.Sequential)]
        private struct MQMGMTPROPS
        {
            public uint cProp;
            public IntPtr aPropID;
            public IntPtr aPropVar;
            public IntPtr status;
        }

#endregion

        private const int MQ_ERROR = unchecked((int)0xC00E0001); // A non-specific Message Queuing error was generated. For example, information about a queue that is currently not the active queue was requested.
        private const int MQ_ERROR_ACCESS_DENIED = unchecked((int)0xC00E0025); // The access rights for retrieving information about the applicable msmq (MSMQ-Configuration) or queue object are not allowed for the calling process.
        private const int MQ_ERROR_ILLEGAL_FORMATNAME = unchecked((int)0xC00E001E); // The specified format name in pObjectName is illegal.
        private const int MQ_ERROR_ILLEGAL_PROPERTY_VT = unchecked((int)0xC00E0019); // An invalid type indicator was supplied for one of the properties specified in pMgmtProps.
        private const int MQ_ERROR_QUEUE_NOT_ACTIVE = unchecked((int)0xC00E0004); // The queue is not open or may not exist.
        private const int MQ_ERROR_SERVICE_NOT_AVAILABLE = unchecked((int)0xC00E000B); // The Message Queuing service is not available.
        // ReSharper disable once RedundantOverflowCheckingContext
        private const int MQ_INFORMATION_UNSUPPORTED_PROPERTY = unchecked((int)0x400E0004); // An unsupported property identifier was specified in pMgmtProps

        const string QueueRegex = @"^(?:(.*\:)|)((?<computerName>[^\\]*)|\.)(?:\\(?<queueType>.*)|)\\(?<queue>.*)$";
        private static readonly Regex regex = new Regex(QueueRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static long GetCount(this MessageQueue messageQueue)
        {
            var match = GetQueuePathMatch(messageQueue.Path);

            var computerName = match.Groups["computerName"].Value;
            var queueType = match.Groups["queueType"].Value;
            var queue = match.Groups["queue"].Value;

            if (computerName == ".")
                computerName = null;

            return GetQueueCount(computerName, queueType, queue);
        }

        internal static Match GetQueuePathMatch(string queuePath)
        {
            var matches = regex.Matches(queuePath);
            if (matches.Count != 1)
            {
                throw new InvalidOperationException($"Unable to parse queue path '{queuePath}'");
            }

            return matches[0];
        }

        private static long GetQueueCount(string computerName, string queueType, string queue)
        {
            if (string.IsNullOrEmpty(computerName)) computerName = null;
            string queuePath = $"queue=Direct=OS:{computerName ?? "."}";

            if (!String.IsNullOrEmpty(queueType))
            {
                queuePath += $"\\{queueType}";
            }

            queuePath += $"\\{queue}";

            return GetCount(computerName, queuePath);
        }

        private static long GetCount(string computerName, string queuePath)
        {
            var props = new MQMGMTPROPS
            {
                cProp = 1,
                aPropID = Marshal.AllocHGlobal(sizeof(int)),
                aPropVar = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MQPROPVariant))),
                status = Marshal.AllocHGlobal(sizeof(int))
            };

            Marshal.WriteInt32(props.aPropID, PROPID_MGMT_QUEUE_MESSAGE_COUNT);
            Marshal.StructureToPtr(new MQPROPVariant { vt = VT_NULL }, props.aPropVar, false);
            Marshal.WriteInt32(props.status, 0);

            try
            {
                int result = MQMgmtGetInfo(computerName, queuePath, ref props);
                //Console.WriteLine("{0} {1} Result:{2:X}", computerName, queuePath, result);
                switch (result)
                {
                    case 0:
                        break;
                    case MQ_ERROR_QUEUE_NOT_ACTIVE:
                        return 0;
                    default:
                        throw new Win32Exception(result);
                }

                if (Marshal.ReadInt32(props.status) != 0)
                    return -1;

                var variant = (MQPROPVariant)Marshal.PtrToStructure(props.aPropVar, typeof(MQPROPVariant));
                if (variant.vt != VT_UI4)
                    return -2;

                return variant.ulVal;
            }
            finally
            {
                Marshal.FreeHGlobal(props.aPropID);
                Marshal.FreeHGlobal(props.aPropVar);
                Marshal.FreeHGlobal(props.status);
            }
        }
    }
}

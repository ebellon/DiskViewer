using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using DWORD = System.UInt32;
using QWORD = System.UInt64;
using TCHAR = System.String;

using BYTE = System.Byte;
using HANDLE = System.IntPtr;
using LPCSTR = System.String;
using SECTORNUM = System.UInt64;

namespace LiatMANET
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FILETIME
    {
        public DWORD dwLowDateTime;
        public DWORD dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct STORAGEDEVICEINFO
    {
        public DWORD cbSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szProfile;
        public DWORD dwDeviceClass;
        public DWORD dwDeviceType;
        public DWORD dwDeviceFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct STOREINFO
    {
        public DWORD cbSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string szDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szStoreName;
        public DWORD dwDeviceClass;
        public DWORD dwDeviceType;
        public STORAGEDEVICEINFO sdi;
        public DWORD dwDeviceFlags;
        public SECTORNUM snNumSectors;
        public DWORD dwBytesPerSector;
        public SECTORNUM snFreeSectors;
        public SECTORNUM snBiggestPartCreatable;
        public FILETIME ftCreated;
        public FILETIME ftLastModified;
        public DWORD dwAttributes;
        public DWORD dwPartitionCount;
        public DWORD dwMountCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct PARTINFO
    {
        public DWORD cbSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPartitionName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szFileSys;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szVolumeName;
        public SECTORNUM snNumSectors;
        public FILETIME ftCreated;
        public FILETIME ftLastModified;
        public DWORD dwAttributes;
        public BYTE bPartType;
    }


    public static unsafe class StorageManager
    {
        [DllImport("Coredll.dll", SetLastError = true)]
        public static extern HANDLE FindFirstStore(ref STOREINFO storeInfo);

        [DllImport("Coredll.dll", SetLastError = true)]
        public static extern bool FindNextStore(HANDLE hSearch, ref STOREINFO storeInfo);

        [DllImport("Coredll.dll", SetLastError = true)]
        public static extern bool FindCloseStore(HANDLE hSearch);

        [DllImport("Coredll.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern HANDLE OpenStore(LPCSTR szDeviceName);

        [DllImport("Coredll.dll", SetLastError = true)]
        public static extern HANDLE FindFirstPartition(HANDLE hStore,ref PARTINFO pPartInfo);

        [DllImport("Coredll.dll", SetLastError = true)]
        public static extern bool FindNextPartition(HANDLE hStore, ref PARTINFO pPartInfo);

        [DllImport("Coredll.dll", SetLastError = true)]
        public static extern bool FindClosePartition(HANDLE hStore);
    }


    unsafe class Program
    {
        /* attributes of a store */
        const DWORD STORE_ATTRIBUTE_READONLY = 0x00000001;
        const DWORD STORE_ATTRIBUTE_REMOVABLE = 0x00000002;
        const DWORD STORE_ATTRIBUTE_UNFORMATTED = 0x00000004;
        const DWORD STORE_ATTRIBUTE_AUTOFORMAT = 0x00000008;
        const DWORD STORE_ATTRIBUTE_AUTOPART = 0x00000010;
        const DWORD STORE_ATTRIBUTE_AUTOMOUNT = 0x00000020;

        /* attributes for a partition */
        const DWORD PARTITION_ATTRIBUTE_EXPENDABLE = 0x00000001;  // partition may be trashed
        const DWORD PARTITION_ATTRIBUTE_READONLY = 0x00000002;  // partition is read-only
        const DWORD PARTITION_ATTRIBUTE_AUTOFORMAT = 0x00000004;
        const DWORD PARTITION_ATTRIBUTE_ACTIVE = 0x00000008;
        const DWORD PARTITION_ATTRIBUTE_BOOT = 0x00000008;  // Active(DOS) == Boot(CE)
        const DWORD PARTITION_ATTRIBUTE_MOUNTED = 0x00000010;


        const DWORD STORAGE_DEVICE_CLASS_BLOCK = 0x1;
        const DWORD STORAGE_DEVICE_CLASS_MULTIMEDIA = 0x2;

        const DWORD STORAGE_DEVICE_TYPE_PCIIDE = (1 << 0);
        const DWORD STORAGE_DEVICE_TYPE_FLASH = (1 << 1);
        const DWORD STORAGE_DEVICE_TYPE_ATA = (1 << 2);
        const DWORD STORAGE_DEVICE_TYPE_ATAPI = (1 << 4);
        const DWORD STORAGE_DEVICE_TYPE_DEPRECATED1 = (1 << 5);
        const DWORD STORAGE_DEVICE_TYPE_DEPRECATED2 = (1 << 6);
        const DWORD STORAGE_DEVICE_TYPE_SRAM = (1 << 7);
        const DWORD STORAGE_DEVICE_TYPE_DVD = (1 << 8);
        const DWORD STORAGE_DEVICE_TYPE_CDROM = (1 << 9);
        const DWORD STORAGE_DEVICE_TYPE_USB = (1 << 10);
        const DWORD STORAGE_DEVICE_TYPE_1394 = (1 << 11);
        const DWORD STORAGE_DEVICE_TYPE_DOC = (1 << 12);
        const DWORD STORAGE_DEVICE_TYPE_UNKNOWN = (1 << 29);
        const DWORD STORAGE_DEVICE_TYPE_REMOVABLE_DRIVE = (1 << 30); // Drive itself is removable
        const DWORD STORAGE_DEVICE_TYPE_REMOVABLE_MEDIA = UInt32.MaxValue; // Just the media is removable ex. CDROM, FLOPPY

        const DWORD STORAGE_DEVICE_FLAG_READWRITE = (1 << 0);
        const DWORD STORAGE_DEVICE_FLAG_READONLY = (1 << 1);
        const DWORD STORAGE_DEVICE_FLAG_TRANSACTED = (1 << 2);
        const DWORD STORAGE_DEVICE_FLAG_MEDIASENSE = (1 << 3); // Device requires media sense calls
        const DWORD STORAGE_DEVICE_FLAG_XIP = (1 << 4);


        // Main Method 
        public static void Main(String[] args)
        {
            Console.WriteLine("Starting up...");
            var storageInformation = new StorageInformation();
            storageInformation.DisplayDisks();
        }


        public class StorageInformation
        {
            HANDLE INVALID_HANDLE_VALUE = (HANDLE)(-1);

            public void DisplayDisks()
            {
                STOREINFO si = new STOREINFO();
                HANDLE hSearch = INVALID_HANDLE_VALUE;

                si.cbSize = (uint)Marshal.SizeOf(typeof(STOREINFO));

                // enumerate first store
                hSearch = StorageManager.FindFirstStore(ref si);

                if (INVALID_HANDLE_VALUE != hSearch)
                {
                    do
                    {
                        Console.WriteLine("Device Name " + si.szDeviceName);
                        Console.WriteLine("Name " + si.szStoreName);
                        var storageClass = "STORAGE_DEVICE_CLASS_MULTIMEDIA";

                        if (si.dwDeviceClass == STORAGE_DEVICE_CLASS_BLOCK)
                        {
                            storageClass = "STORAGE_DEVICE_CLASS_BLOCK";
                        }

                        Console.WriteLine("Class " + storageClass);
                        Console.WriteLine("Type ");

                        switch (si.dwDeviceType)
                        {
                            case STORAGE_DEVICE_TYPE_PCIIDE:
                                Console.WriteLine("STORAGE_DEVICE_TYPE_PCIIDE\n");
                                break;
                            case STORAGE_DEVICE_TYPE_FLASH:
                                Console.WriteLine("STORAGE_DEVICE_TYPE_FLASH\n");
                                break;
                            case STORAGE_DEVICE_TYPE_ATA:
                                Console.WriteLine("STORAGE_DEVICE_TYPE_ATA\n");
                                break;
                            case STORAGE_DEVICE_TYPE_ATAPI:
                                Console.WriteLine("STORAGE_DEVICE_TYPE_ATAPI\n");
                                break;
                            case STORAGE_DEVICE_TYPE_SRAM:
                                Console.WriteLine("STORAGE_DEVICE_TYPE_SRAM\n");
                                break;
                            case STORAGE_DEVICE_TYPE_DVD:
                                Console.WriteLine("STORAGE_DEVICE_TYPE_DVD\n");
                                break;
                            case STORAGE_DEVICE_TYPE_CDROM:
                                Console.WriteLine("STORAGE_DEVICE_TYPE_CDROM\n");
                                break;
                            case STORAGE_DEVICE_TYPE_USB:
                                Console.WriteLine("STORAGE_DEVICE_TYPE_USB\n");
                                break;
                            case STORAGE_DEVICE_TYPE_1394:
                                Console.WriteLine("STORAGE_DEVICE_TYPE_1394\n");
                                break;
                            case STORAGE_DEVICE_TYPE_DOC:
                                Console.WriteLine("STORAGE_DEVICE_TYPE_DOC\n");
                                break;
                            case STORAGE_DEVICE_TYPE_UNKNOWN:
                                Console.WriteLine("STORAGE_DEVICE_TYPE_UNKNOWN\n");
                                break;
                            case STORAGE_DEVICE_TYPE_REMOVABLE_DRIVE:
                                Console.WriteLine("STORAGE_DEVICE_TYPE_REMOVABLE_DRIVE\n");
                                break;
                            case STORAGE_DEVICE_TYPE_REMOVABLE_MEDIA:
                                Console.WriteLine("STORAGE_DEVICE_TYPE_REMOVABLE_MEDIA\n");
                                break;
                            default:
                                Console.WriteLine("Unkown device type " + si.dwDeviceType);
                                break;
                        }

                        Console.WriteLine("Flags");
                        if ((si.dwDeviceFlags & STORAGE_DEVICE_FLAG_READWRITE) != 0)
                            Console.WriteLine("STORAGE_DEVICE_FLAG_READWRITE");
                        if ((si.dwDeviceFlags & STORAGE_DEVICE_FLAG_READONLY) != 0)
                            Console.WriteLine("STORAGE_DEVICE_FLAG_READONLY");
                        if ((si.dwDeviceFlags & STORAGE_DEVICE_FLAG_TRANSACTED) != 0)
                            Console.WriteLine(" STORAGE_DEVICE_FLAG_TRANSACTED");
                        if ((si.dwDeviceFlags & STORAGE_DEVICE_FLAG_MEDIASENSE) != 0)
                            Console.WriteLine("STORAGE_DEVICE_FLAG_MEDIASENSE");
                        if (si.dwDeviceFlags == 0)
                            Console.WriteLine(" None");


                        Console.WriteLine("Bytes per Sector " + si.dwBytesPerSector);
                        Console.WriteLine("Attributes");
                        if ((si.dwAttributes & STORE_ATTRIBUTE_READONLY) != 0)
                            Console.WriteLine(" STORE_ATTRIBUTE_READONLY");
                        if ((si.dwAttributes & STORE_ATTRIBUTE_REMOVABLE) != 0)
                            Console.WriteLine(" STORE_ATTRIBUTE_REMOVABLE");
                        if ((si.dwAttributes & STORE_ATTRIBUTE_UNFORMATTED) != 0)
                            Console.WriteLine(" STORE_ATTRIBUTE_UNFORMATTED");
                        if ((si.dwAttributes & STORE_ATTRIBUTE_AUTOFORMAT) != 0)
                            Console.WriteLine("STORE_ATTRIBUTE_AUTOFORMAT");
                        if ((si.dwAttributes & STORE_ATTRIBUTE_AUTOPART) != 0)
                            Console.WriteLine("STORE_ATTRIBUTE_AUTOPART");
                        if ((si.dwAttributes & STORE_ATTRIBUTE_AUTOMOUNT) != 0)
                            Console.WriteLine("STORE_ATTRIBUTE_AUTOMOUNT");
                        if (si.dwAttributes == 0)
                            Console.WriteLine("None");


                        Console.WriteLine("Partition Count " + si.dwPartitionCount);
                        Console.WriteLine("Mount Count " + si.dwMountCount);


                        HANDLE hStore = StorageManager.OpenStore(si.szDeviceName);

                        if (hStore != INVALID_HANDLE_VALUE)
                        {
                            DisplayPartions(hStore);
                        }
                        else
                        {
                            Console.WriteLine("OpenSelectedStore failed");
                        }

                    }
                    while (StorageManager.FindNextStore(hSearch, ref si));
                    StorageManager.FindCloseStore(hSearch);
                }
            }

            void DisplayPartions(HANDLE hStore)
            {
                HANDLE hSearch = INVALID_HANDLE_VALUE;
                HANDLE hPartition = INVALID_HANDLE_VALUE;
                PARTINFO partInfo = new PARTINFO();

                partInfo.cbSize = (uint)Marshal.SizeOf(typeof(PARTINFO));
                
                hSearch = StorageManager.FindFirstPartition(hStore, ref partInfo);

                if (INVALID_HANDLE_VALUE != hSearch)
                {
                    do
                    {
                        if ((PARTITION_ATTRIBUTE_MOUNTED & partInfo.dwAttributes) != 0)
                        {
                            Console.WriteLine(string.Format("\t\tPartition Mounted"));

                        }
                        else
                        {
                            Console.WriteLine(string.Format("\t\tPartition NOT Mounted"));
                        }

                        Console.WriteLine(string.Format("\t\tPartition Name {0}\n", partInfo.szPartitionName));
                        //Console.WriteLine(string.Format("\t\tSize  {0}\n", partInfo.cbSize));
                        Console.WriteLine(string.Format("\t\tFile System {0}\n", partInfo.szFileSys));
                        Console.WriteLine(string.Format("\t\tVolume Name {0}\n", partInfo.szVolumeName));
                        Console.WriteLine(string.Format("\t\tNumber of Sectors {0}\n", partInfo.snNumSectors));
                        Console.WriteLine(string.Format("\t\tAttributes"));
                        if ((partInfo.dwAttributes & PARTITION_ATTRIBUTE_EXPENDABLE) != 0)
                            Console.WriteLine(" PARTITION_ATTRIBUTE_EXPENDABLE");
                        if ((partInfo.dwAttributes & PARTITION_ATTRIBUTE_READONLY) != 0)
                            Console.WriteLine(" PARTITION_ATTRIBUTE_READONLY");
                        if ((partInfo.dwAttributes & PARTITION_ATTRIBUTE_BOOT) != 0)
                            Console.WriteLine(" PARTITION_ATTRIBUTE_BOOT");
                        if ((partInfo.dwAttributes & PARTITION_ATTRIBUTE_AUTOFORMAT) != 0)
                            Console.WriteLine(" PARTITION_ATTRIBUTE_AUTOFORMAT");
                        if ((partInfo.dwAttributes & PARTITION_ATTRIBUTE_MOUNTED) != 0)
                            Console.WriteLine(" PARTITION_ATTRIBUTE_MOUNTED");
                        if ((partInfo.dwAttributes & 0) != 0)
                            Console.WriteLine(" None");
                        Console.WriteLine("\n");


                        // Convert the partion type value to a string
                        var bpartType = string.Format("{0:X2}", partInfo.bPartType);
                        Console.WriteLine("Partition type code " + bpartType);

                        using (var registryKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("System\\StorageManager\\PartitionTable"))
                        {
                            Console.WriteLine("Partition Type value " + registryKey.GetValue(bpartType));
                        }

                    }
                    while (StorageManager.FindNextPartition(hSearch, ref partInfo));
                    StorageManager.FindClosePartition(hSearch);
                }
            }
        }

    }
}

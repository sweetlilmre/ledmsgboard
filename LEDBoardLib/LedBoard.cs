using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Threading;

namespace LEDBoardLib
{
  public class FontData
  {
    public int Width
    {
      get; private set;
    }
    
    public int Height
    {
      get;
      private set;
    }

    public Dictionary<char, byte[]> Data
    {
      get; private set;
    }

    public FontData(int width, int height, Dictionary<char, byte[]> data)
    {
      Width = width;
      Height = height;
      Data = data;
    }

  }
  public class SymbolData
  {
    public char id;
    public byte[] data;
  }

  public class LEDBoard
  {
    #region INTEROP CODE

    public const Int32 INVALID_HANDLE_VALUE = -1;
    public const Int32 ERROR_INSUFFICIENT_BUFFER = 122;
    
    protected const uint GENERIC_READ = 0x80000000;
    protected const uint GENERIC_WRITE = 0x40000000;
    protected const uint FILE_SHARE_WRITE = 0x2;
    protected const uint FILE_SHARE_READ = 0x1;
    protected const uint OPEN_EXISTING = 3;


    [Flags]
    public enum DIGCF : int
    {
      DEFAULT = 0x00000001,
      PRESENT = 0x00000002,
      ALLCLASSES = 0x00000004,
      PROFILE = 0x00000008,
      DEVICEINTERFACE = 0x00000010,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVICE_INTERFACE_DATA
    {
      public Int32 cbSize;
      public Guid interfaceClassGuid;
      public Int32 flags;
      private UIntPtr reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDD_ATTRIBUTES
    {
      public Int32 Size;
      public Int16 VendorID;
      public Int16 ProductID;
      public Int16 VersionNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVINFO_DATA
    {
      public uint cbSize;
      public Guid ClassGuid;
      public uint DevInst;
      public IntPtr Reserved;
    }

    // Device interface detail data
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SP_DEVICE_INTERFACE_DETAIL_DATA
    {
      public UInt32 cbSize;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
      public string DevicePath;
    }

    [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid,
      IntPtr enumerator,
      IntPtr hwndParent,
      [MarshalAs(UnmanagedType.U4)] DIGCF Flags);

    [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr hDevInfo);

    [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern Boolean SetupDiEnumDeviceInterfaces(IntPtr hDevInfo,
      IntPtr deviceInfoData,
      ref Guid interfaceClassGuid,
      UInt32 memberIndex,
      ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

    public static Boolean SetupDiEnumDeviceInterfaces(IntPtr hDevInfo,
      SP_DEVINFO_DATA deviceInfoData,
      ref Guid interfaceClassGuid,
      UInt32 memberIndex,
      ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData)
    {
      return (SetupDiEnumDeviceInterfaces(hDevInfo, deviceInfoData, ref interfaceClassGuid, memberIndex, ref deviceInterfaceData));
    }

    [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern Boolean SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo,
      ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
      IntPtr deviceInterfaceDetailData,
      UInt32 deviceInterfaceDetailDataSize,
      out UInt32 requiredSize,
      IntPtr deviceInfoData
    );

    [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern Boolean SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo,
      ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
      ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
      UInt32 deviceInterfaceDetailDataSize,
      out UInt32 requiredSize,
      IntPtr deviceInfoData
    );

    [DllImport(@"hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern void HidD_GetHidGuid(ref Guid classGuid);

    [DllImport(@"hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern Boolean HidD_GetAttributes(IntPtr HidDeviceObject,
      ref HIDD_ATTRIBUTES Attributes);

    [DllImport("kernel32.dll", SetLastError = true)]
    protected static extern IntPtr CreateFile([MarshalAs(UnmanagedType.LPStr)] string strName, 
      UInt32 nAccess, 
      UInt32 nShareMode, 
      IntPtr lpSecurity, 
      UInt32 nCreationFlags, 
      UInt32 nAttributes, 
      IntPtr lpTemplate);

    [DllImport("kernel32.dll", SetLastError = true)]
    protected static extern int CloseHandle(IntPtr hFile);

    #endregion

    #region SYMBOL TABLE


    #endregion

    public const UInt32 VENDOR_ID = 0x1D34;
    public const UInt32 PRODUCT_ID = 0x13;

    private FileStream _hidStream;
    byte[][] table = new byte[4][];

    private FontData Font;

    public LEDBoard(FontData font)
    {
      _hidStream = null;
      table[0] = new byte[9] { 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
      table[1] = new byte[9] { 0x00, 0x00, 0x02, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
      table[2] = new byte[9] { 0x00, 0x00, 0x04, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
      table[3] = new byte[9] { 0x00, 0x00, 0x06, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
      
      Font = font;
    }

    public void DrawChar(char ch, int x, int y)
    {
      if (Font.Data.ContainsKey(ch))
      {
        byte[] data = Font.Data[ch];

        for (int ypos = y; ypos < y + Font.Height; ypos++)
        {
          byte val = data[ypos - y];
          for (int xpos = x; xpos < x + Font.Width; xpos++)
          {
            if ((val & (1 << (Font.Width - (xpos - x)))) != 0)
              SetPixel(xpos, ypos);
            else
            {
              ClearPixel(xpos, ypos);
            }
          }
        }
      }
    }


    public void DrawString(string s, int x, int y)
    {
      foreach (char c in s)
      {
        DrawChar(c, x, y);
        x += 5;
      }
    }

    public void ChangePixel(int x, int y, bool on)
    {
      if (y < 0 || y > 6)
        return;

      if (x < 0 || x > 20)
        return;

      // 0 - 3
      int yindex = y / 2;
      int xindex = y % 2 == 0 ? 3 : 6;

      byte xbit = (byte) x;
      if (x < 8)
      {
        xindex += 2;
      }
      else if (x < 16)
      {
        xindex += 1;
        xbit -= 8;
      }
      else
      {
        xbit -= 16;
      }

      byte val = table[yindex][xindex];
      if (on)
      {
        val &= (byte) ~(1 << xbit);
      }
      else
      {
        val |= (byte)(1 << xbit);
      }
      table[yindex][xindex] = val;

    }

    public void SetPixel(int x, int y)
    {
      ChangePixel(x, y, true);
    }

    public void ClearPixel(int x, int y)
    {
      ChangePixel(x, y, false);
    }

    public void Update()
    {
      for (int n = 0; n < 4; n++)
      {
        _hidStream.Write(table[n], 0, 9);
      }
    }

    public bool Init()
    {
      Guid ClassGuid = Guid.Empty;
      HidD_GetHidGuid(ref ClassGuid);

      IntPtr hDevInfo = SetupDiGetClassDevs(ref ClassGuid, IntPtr.Zero, IntPtr.Zero, DIGCF.DEVICEINTERFACE | DIGCF.PRESENT);
      if (hDevInfo.ToInt32() == INVALID_HANDLE_VALUE)
        return (false);

      bool Success = true;
      UInt32 memberIndex = 0;
      while (Success)
      {
        // create a Device Interface Data structure
        SP_DEVICE_INTERFACE_DATA DeviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
        DeviceInterfaceData.cbSize = Marshal.SizeOf(DeviceInterfaceData);

        IntPtr deviceInfoData = IntPtr.Zero;
        // start the enumeration
        Success = SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref ClassGuid, memberIndex, ref DeviceInterfaceData);
        if (Success)
        {
          // build a Device Interface Detail Data structure
          SP_DEVICE_INTERFACE_DETAIL_DATA didd = new SP_DEVICE_INTERFACE_DETAIL_DATA();
          if (IntPtr.Size == 8) // for 64 bit operating systems
            didd.cbSize = 8;
          else
            didd.cbSize = (UInt32)(4 + Marshal.SystemDefaultCharSize); // for 32 bit systems

          // now we can get some more detailed information
          UInt32 nRequiredSize = 0;
          IntPtr temp = IntPtr.Zero;

          SetupDiGetDeviceInterfaceDetail(hDevInfo, ref DeviceInterfaceData, IntPtr.Zero, 0, out nRequiredSize, IntPtr.Zero);
          if (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
          {
            if (SetupDiGetDeviceInterfaceDetail(hDevInfo, ref DeviceInterfaceData, ref didd, nRequiredSize, out nRequiredSize, IntPtr.Zero))
            {
              IntPtr hFile = CreateFile(didd.DevicePath, GENERIC_READ|GENERIC_WRITE, FILE_SHARE_READ|FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
              if (hFile.ToInt32() != INVALID_HANDLE_VALUE)
              {
                HIDD_ATTRIBUTES hiddAttrs = new HIDD_ATTRIBUTES();
                HidD_GetAttributes(hFile, ref hiddAttrs);
                if (hiddAttrs.VendorID == VENDOR_ID && hiddAttrs.ProductID == PRODUCT_ID)
                {
                  _hidStream = new FileStream(new SafeFileHandle(hFile, false), FileAccess.Read | FileAccess.Write, 9, false);
                  break;
                }
                CloseHandle(hFile);
              }
            }
          }
          memberIndex++;
        }
      }

      SetupDiDestroyDeviceInfoList(hDevInfo);
      return (Success);
    }
  }
}

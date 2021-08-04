using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
public static class Keyboard {
  
    [DllImport("user32.dll")]
    private static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    const int KEYEVENTF_EXTENDEDKEY = 0x0001;
    const int KEYEVENTF_KEYUP = 0x0002;
    const int KEY_TOGGLED = 0x0001;
    const int KEY_PRESSED = 0x8000;
    const int ACTION_DELAY = 1;
    //const int WM_KEYUP = 0x0101;
    //const int WM_SYSKEYUP = 0x0105;

    public static void KeyUp(Keys key) {
        keybd_event((byte)key, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0); //0x7F
    }

    /// <summary>
    ///it is safe. Default delay - 150 ms
    /// </summary>
    /// <param name="key"></param>
    public static void KeyPress(Keys key, string from = null) {
        KeyDown(key, from);
        Thread.Sleep(ACTION_DELAY);
        KeyUp(key);
    }

    static double mdi = 150;  //minimal_down_interval  //ms
    public delegate void MDownInfoDelegate(string write);
    static ConcurrentDictionary<Keys, (Stopwatch, int)> using_keys =
        new ConcurrentDictionary<Keys, (Stopwatch, int)>();
    public static void KeyDown(Keys _key, string _from = null) {
        if(!using_keys.ContainsKey(_key))
            using_keys.TryAdd(_key, (new Stopwatch(), 0));
        var elaps = using_keys[_key].Item1.Elapsed.TotalMilliseconds;
        if(elaps < mdi) {
            Thread.Sleep((int)(mdi - elaps));
            // act?.Invoke("SafeLeftDown err: so fast="+ elaps);
            var last = using_keys[_key];
            using_keys[_key] = (last.Item1, last.Item2 += 1);
           
            using_keys[_key].Item1.Restart();
        }
        keybd_event((byte)_key, 0, KEYEVENTF_EXTENDEDKEY | 0, 0);
    }
    [DllImport("USER32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    static ConcurrentDictionary<Keys, DateTime> down_time = new ConcurrentDictionary<Keys, DateTime>();
    public static bool HotKeyPressed(Keys key, int interv = 300, bool debug = false) {
        if((GetKeyState((int)key) & KEY_PRESSED) != 0) {
            if(!down_time.ContainsKey(key) || (down_time.ContainsKey(key) && down_time[key].AddMilliseconds(interv) < DateTime.Now)) {
                down_time[key] = DateTime.Now;
            
                return true;
            }
            else {
                //if(debug && AddToLog != null)
                //    AddToLog("HotKey " + key.ToString() + " was already pressed recently");
                return false;
            }
        }
        return false;
    }

    public static bool IsKeyDown(Keys key) {
        return GetKeyState((int)key) < 0;
    }

  

    public static bool IsKeyPressed(Keys key) {
        return Convert.ToBoolean(GetKeyState((int)key) & KEY_PRESSED);
    }

    public static bool IsKeyToggled(Keys key) {
        return Convert.ToBoolean(GetKeyState((int)key) & KEY_TOGGLED);
    }
}
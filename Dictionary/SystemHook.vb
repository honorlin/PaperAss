Imports System.Reflection, System.Threading, System.ComponentModel, System.Runtime.InteropServices
''' <summary>本類可以在.NET環境下使用系統鍵盤與滑鼠鉤子</summary>
Public Class SystemHook
#Region "定義結構"
    Private Structure MouseHookStruct
        Dim PT As POINT
        Dim Hwnd As Integer
        Dim WHitTestCode As Integer
        Dim DwExtraInfo As Integer
    End Structure
    Private Structure MouseLLHookStruct
        Dim PT As POINT
        Dim MouseData As Integer
        Dim Flags As Integer
        Dim Time As Integer
        Dim DwExtraInfo As Integer
    End Structure
    Private Structure KeyboardHookStruct
        Dim vkCode As Integer
        Dim ScanCode As Integer
        Dim Flags As Integer
        Dim Time As Integer
        Dim DwExtraInfo As Integer
    End Structure
#End Region
#Region "API聲明導入"
    Private Declare Function SetWindowsHookExA Lib "user32" (ByVal idHook As Integer, ByVal lpfn As HookProc, ByVal hMod As IntPtr, ByVal dwThreadId As Integer) As Integer
    Private Declare Function UnhookWindowsHookEx Lib "user32" (ByVal idHook As Integer) As Integer
    Private Declare Function CallNextHookEx Lib "user32" (ByVal idHook As Integer, ByVal nCode As Integer, ByVal wParam As Integer, ByVal lParam As IntPtr) As Integer
    Private Declare Function ToAscii Lib "user32" (ByVal uVirtKey As Integer, ByVal uScancode As Integer, ByVal lpdKeyState As Byte(), ByVal lpwTransKey As Byte(), ByVal fuState As Integer) As Integer
    Private Declare Function GetKeyboardState Lib "user32" (ByVal pbKeyState As Byte()) As Integer
    Private Declare Function GetKeyState Lib "user32" (ByVal vKey As Integer) As Short
    Private Delegate Function HookProc(ByVal nCode As Integer, ByVal wParam As Integer, ByVal lParam As IntPtr) As Integer
#End Region
#Region "常量聲明"
    Private Const WH_MOUSE_LL = 14
    Private Const WH_KEYBOARD_LL = 13
    Private Const WH_MOUSE = 7
    Private Const WH_KEYBOARD = 2
    Private Const WM_MOUSEMOVE = &H200
    Private Const WM_LBUTTONDOWN = &H201
    Private Const WM_RBUTTONDOWN = &H204
    Private Const WM_MBUTTONDOWN = &H207
    Private Const WM_LBUTTONUP = &H202
    Private Const WM_RBUTTONUP = &H205
    Private Const WM_MBUTTONUP = &H208
    Private Const WM_LBUTTONDBLCLK = &H203
    Private Const WM_RBUTTONDBLCLK = &H206
    Private Const WM_MBUTTONDBLCLK = &H209
    Private Const WM_MOUSEWHEEL = &H20A
    Private Const WM_KEYDOWN = &H100
    Private Const WM_KEYUP = &H101
    Private Const WM_SYSKEYDOWN = &H104
    Private Const WM_SYSKEYUP = &H105
    Private Const VK_SHIFT As Byte = &H10
    Private Const VK_CAPITAL As Byte = &H14
    Private Const VK_NUMLOCK As Byte = &H90
#End Region
    ''' <summary>滑鼠啟動事件</summary>
    Public Event MouseActivity As MouseEventHandler
    ''' <summary>鍵盤按下事件</summary>
    Public Event KeyDown As KeyEventHandler
    ''' <summary>鍵盤輸入事件</summary>
    Public Event KeyPress As KeyPressEventHandler
    ''' <summary>鍵盤鬆開事件</summary>
    Public Event KeyUp As KeyEventHandler
    Private hMouseHook As Integer
    Private hKeyboardHook As Integer
    Private Shared MouseHookProcedure As HookProc
    Private Shared KeyboardHookProcedure As HookProc
    ''' <summary>創建一個全域滑鼠鍵盤鉤子 (請使用Start方法開始監視)</summary>
    Sub New()
        '留空即可
    End Sub
    ''' <summary>創建一個全域滑鼠鍵盤鉤子，決定是否安裝鉤子</summary>
    ''' <param name="InstallAll">是否立刻掛鉤系統消息</param>
    Sub New(ByVal InstallAll As Boolean)
        If InstallAll Then StartHook(True, True)
    End Sub
    ''' <summary>創建一個全域滑鼠鍵盤鉤子，並決定安裝鉤子的類型</summary>
    ''' <param name="InstallKeyboard">掛鉤鍵盤消息</param>
    ''' <param name="InstallMouse">掛鉤滑鼠消息</param>
    Sub New(ByVal InstallKeyboard As Boolean, ByVal InstallMouse As Boolean)
        StartHook(InstallKeyboard, InstallMouse)
    End Sub
    ''' <summary>析構函數</summary>
    Protected Overrides Sub Finalize()
        UnHook() '卸載物件時反註冊系統鉤子
        MyBase.Finalize()
    End Sub
    ''' <summary>開始安裝系統鉤子</summary>
    ''' <param name="InstallKeyboardHook">掛鉤鍵盤消息</param>
    ''' <param name="InstallMouseHook">掛鉤滑鼠消息</param>
    Public Sub StartHook(Optional ByVal InstallKeyboardHook As Boolean = True, Optional ByVal InstallMouseHook As Boolean = False)
        '註冊鍵盤鉤子
        If InstallKeyboardHook AndAlso hKeyboardHook = 0 Then
            KeyboardHookProcedure = New HookProc(AddressOf KeyboardHookProc)
            hKeyboardHook = SetWindowsHookExA(WH_KEYBOARD_LL, KeyboardHookProcedure, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly.GetModules()(0)), 0)
            If hKeyboardHook = 0 Then '檢測是否註冊完成
                UnHook(True, False) '在這裡反註冊
                Throw New Win32Exception(Marshal.GetLastWin32Error) '報告錯誤
            End If
        End If
        '註冊滑鼠鉤子
        If InstallMouseHook AndAlso hMouseHook = 0 Then
            MouseHookProcedure = New HookProc(AddressOf MouseHookProc)
            hMouseHook = SetWindowsHookExA(WH_MOUSE_LL, MouseHookProcedure, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly.GetModules()(0)), 0)
            If hMouseHook = 0 Then
                UnHook(False, True)
                Throw New Win32Exception(Marshal.GetLastWin32Error)
            End If
        End If
    End Sub
    ''' <summary>立刻卸載系統鉤子</summary>
    ''' <param name="UninstallKeyboardHook">卸載鍵盤鉤子</param>
    ''' <param name="UninstallMouseHook">卸載滑鼠鉤子</param>
    ''' <param name="ThrowExceptions">是否報告錯誤</param>
    Public Sub UnHook(Optional ByVal UninstallKeyboardHook As Boolean = True, Optional ByVal UninstallMouseHook As Boolean = True, Optional ByVal ThrowExceptions As Boolean = False)
        '卸載鍵盤鉤子
        If hKeyboardHook <> 0 AndAlso UninstallKeyboardHook Then
            Dim retKeyboard As Integer = UnhookWindowsHookEx(hKeyboardHook)
            hKeyboardHook = 0
            If ThrowExceptions AndAlso retKeyboard = 0 Then '如果出現錯誤，是否報告錯誤
                Throw New Win32Exception(Marshal.GetLastWin32Error) '報告錯誤
            End If
        End If
        '卸載滑鼠鉤子
        If hMouseHook <> 0 AndAlso UninstallMouseHook Then
            Dim retMouse As Integer = UnhookWindowsHookEx(hMouseHook)
            hMouseHook = 0
            If ThrowExceptions AndAlso retMouse = 0 Then
                Throw New Win32Exception(Marshal.GetLastWin32Error)
            End If
        End If
    End Sub
    '鍵盤消息的委託處理代碼
    Private Function KeyboardHookProc(ByVal nCode As Integer, ByVal wParam As Integer, ByVal lParam As IntPtr) As Integer
        Dim handled As Boolean = False
        If nCode >= 0 Then
            Dim MyKeyboardHookStruct As KeyboardHookStruct = DirectCast(Marshal.PtrToStructure(lParam, GetType(KeyboardHookStruct)), KeyboardHookStruct)
            '啟動KeyDown
            If wParam = WM_KEYDOWN OrElse wParam = WM_SYSKEYDOWN Then '如果消息為按下普通鍵或系統鍵
                Dim e As New KeyEventArgs(MyKeyboardHookStruct.vkCode)
                RaiseEvent KeyDown(Me, e) '啟動事件
                handled = handled Or e.Handled '是否取消下一個鉤子
            End If
            '啟動KeyUp
            If wParam = WM_KEYUP OrElse wParam = WM_SYSKEYUP Then
                Dim e As New KeyEventArgs(MyKeyboardHookStruct.vkCode)
                RaiseEvent KeyUp(Me, e)
                handled = handled Or e.Handled
            End If
            '啟動KeyPress
            If wParam = WM_KEYDOWN Then
                Dim isDownShift As Boolean = (GetKeyState(VK_SHIFT) & &H80 = &H80)
                Dim isDownCapslock As Boolean = (GetKeyState(VK_CAPITAL) <> 0)
                Dim keyState(256) As Byte
                GetKeyboardState(keyState)
                Dim inBuffer(2) As Byte
                If ToAscii(MyKeyboardHookStruct.vkCode, MyKeyboardHookStruct.scanCode, keyState, inBuffer, MyKeyboardHookStruct.flags) = 1 Then
                    Dim key As Char = Chr(inBuffer(0))
                    If isDownCapslock Xor isDownShift And Char.IsLetter(key) Then
                        key = Char.ToUpper(key)
                    End If
                    Dim e As New KeyPressEventArgs(key)
                    RaiseEvent KeyPress(Me, e)
                    handled = handled Or e.Handled
                End If
            End If
            '取消或者啟動下一個鉤子
            If handled Then Return 1 Else Return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam)
        End If
    End Function
    '滑鼠消息的委託處理代碼
    Private Function MouseHookProc(ByVal nCode As Integer, ByVal wParam As Integer, ByVal lParam As IntPtr) As Integer
        If nCode >= 0 Then
            Dim mouseHookStruct As MouseLLHookStruct = DirectCast(Marshal.PtrToStructure(lParam, GetType(MouseLLHookStruct)), MouseLLHookStruct)
            Dim moubut As MouseButtons = MouseButtons.None '滑鼠按鍵
            Dim mouseDelta As Integer = 0 '滾輪值
            Select Case wParam
                Case WM_LBUTTONDOWN
                    moubut = MouseButtons.Left
                Case WM_RBUTTONDOWN
                    moubut = MouseButtons.Right
                Case WM_MBUTTONDOWN
                    moubut = MouseButtons.Middle
                Case WM_MOUSEWHEEL
                    Dim int As Integer = (mouseHookStruct.mouseData >> 16) And &HFFFF
                    '本段代碼CLE添加，模仿C#的Short從Int棄位轉換
                    If int > Short.MaxValue Then mouseDelta = int - 65536 Else mouseDelta = int
            End Select
            Dim clickCount As Integer = 0 '按一下次數
            If moubut <> MouseButtons.None Then
                If wParam = WM_LBUTTONDBLCLK OrElse wParam = WM_RBUTTONDBLCLK OrElse wParam = WM_MBUTTONDBLCLK Then
                    clickCount = 2
                Else
                    clickCount = 1
                End If
            End If
            Dim e As New MouseEventArgs(moubut, clickCount, mouseHookStruct.pt.x, mouseHookStruct.pt.y, mouseDelta)
            RaiseEvent MouseActivity(Me, e)
        End If
        Return CallNextHookEx(hMouseHook, nCode, wParam, lParam) '啟動下一個鉤子
    End Function
    ''' <summary>鍵盤鉤子是否無效</summary>
    Public Property KeyHookInvalid() As Boolean
        Get
            Return hKeyboardHook = 0
        End Get
        Set(ByVal value As Boolean)
            If value Then UnHook(True, False) Else StartHook(True, False)
        End Set
    End Property
    ''' <summary>滑鼠鉤子是否無效</summary>
    Public Property MouseHookInvalid() As Boolean
        Get
            Return hMouseHook = 0
        End Get
        Set(ByVal value As Boolean)
            If value Then UnHook(False, True) Else StartHook(False, True)
        End Set
    End Property
End Class

﻿

Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Windows.Automation
Imports Microsoft.VisualBasic.CompilerServices
Imports Microsoft.Win32
Imports Transitions



Public Class Form1



    <DllImport("user32.dll", EntryPoint:="FindWindow", SetLastError:=True, CharSet:=CharSet.Auto)>
    Private Shared Function FindWindowByClass(ByVal lpClassName As String, ByVal zero As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetWindowRect(ByVal hWnd As IntPtr, ByRef lpRect As RECT) As Boolean
    End Function

    <DllImport("user32.dll", ExactSpelling:=True, CharSet:=CharSet.Auto)>
    Public Shared Function GetParent(ByVal hWnd As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
    Public Shared Function SetParent(ByVal hWndChild As IntPtr, ByVal hWndNewParent As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SetWindowPos(ByVal hWnd As IntPtr, ByVal hWndInsertAfter As IntPtr, ByVal X As Integer, ByVal Y As Integer, ByVal cx As Integer, ByVal cy As Integer, ByVal uFlags As UInt32) As Boolean
    End Function

    <DllImport("user32.dll")>
    Public Shared Function SendMessage(ByVal hWnd As IntPtr, ByVal wMsg As Int32, ByVal wParam As Boolean, ByVal lParam As Int32) As Integer
    End Function

    <DllImport("kernel32.dll")>
    Private Shared Function SetProcessWorkingSetSize(ByVal hProcess As IntPtr, ByVal dwMinimumWorkingSetSize As Int32, ByVal dwMaximumWorkingSetSize As Int32) As Int32
    End Function

    Dim SWP_NOSIZE As UInt32 = 1
    Dim SWP_NOMOVE As UInt32 = 2
    Dim SWP_NOZORDER As UInt32 = 4
    Dim SWP_NOREDRAW As UInt32 = 8
    Dim SWP_NOACTIVATE As UInt32 = 16
    Dim SWP_DRAWFRAME As UInt32 = 32
    Dim SWP_FRAMECHANGED As UInt32 = 32
    Dim SWP_SHOWWINDOW As UInt32 = 64
    Dim SWP_HIDEWINDOW As UInt32 = 128
    Dim SWP_NOCOPYBITS As UInt32 = 256
    Dim SWP_NOOWNERZORDER As UInt32 = 512
    Dim SWP_NOREPOSITION As UInt32 = 512
    Dim SWP_NOSENDCHANGING As UInt32 = 1024
    Dim SWP_DEFERERASE As UInt32 = 8192
    Dim SWP_ASYNCWINDOWPOS As UInt32 = 16384

    Private Const WM_SETREDRAW As Integer = 11

    Dim HWND_TOP As IntPtr = 0
    Dim HWND_BOTTOM As IntPtr = 1
    Dim HWND_TOPMOST As IntPtr = -1
    Dim HWND_NOTOPMOST As IntPtr = -2
    Public Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure

    Dim Shell_TrayWnd As AutomationElement = AutomationElement.FromHandle(FindWindowByClass("Shell_TrayWnd", 0))
    Dim MSTaskListWClass As AutomationElement = Shell_TrayWnd.FindFirst(TreeScope.Descendants, New PropertyCondition(AutomationElement.ClassNameProperty, "MSTaskListWClass"))
    Dim TrayNotifyWnd As AutomationElement = Shell_TrayWnd.FindFirst(TreeScope.Descendants, New PropertyCondition(AutomationElement.ClassNameProperty, "TrayNotifyWnd"))
    Dim MSTaskSwWClass = GetParent(MSTaskListWClass.Current.NativeWindowHandle)
    Dim ReBarWindow32 = GetParent(MSTaskSwWClass)
    Dim Desktop = GetParent(FindWindowByClass("Shell_TrayWnd", 0))

    Dim DesktopPtr As IntPtr = Desktop
    Dim Shell_TrayWndPtr As IntPtr = Shell_TrayWnd.Current.NativeWindowHandle
    Dim MSTaskListWClassPtr As IntPtr = MSTaskListWClass.Current.NativeWindowHandle
    Dim TrayNotifyWndPtr As IntPtr = TrayNotifyWnd.Current.NativeWindowHandle
    Dim MSTaskSwWClassPtr As IntPtr = MSTaskSwWClass
    Dim ReBarWindow32Ptr As IntPtr = ReBarWindow32

    Dim TaskbarWidthFull As Integer
    Dim TaskbarLeft As Integer

    Dim IsTaskbarMoving As Boolean
    Dim TaskbarNewPos As Integer

    Sub RestartExplorer()
        For Each MyProcess In Process.GetProcessesByName("explorer")
            MyProcess.Kill()
        Next
    End Sub


    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        If RadioButton1.Checked = False AndAlso RadioButton2.Checked = False AndAlso RadioButton3.Checked = False AndAlso RadioButton4.Checked = False AndAlso RadioButton5.Checked = False AndAlso RadioButton6.Checked = False Then
            RadioButton1.Checked = True
        End If

        RunAtStartUp()

        Console.WriteLine("Desktop Hwnd: " & DesktopPtr.ToString)
        Console.WriteLine("Shell_TrayWnd Hwnd: " & Shell_TrayWndPtr.ToString)
        Console.WriteLine("MSTaskListWClass Hwnd: " & MSTaskListWClassPtr.ToString)
        Console.WriteLine("MSTaskSwWClass Hwnd: " & MSTaskSwWClassPtr.ToString)
        Console.WriteLine("ReBarWindow32 Hwnd: " & ReBarWindow32Ptr.ToString)
        Console.WriteLine("TrayNotifyWnd Hwnd: " & TrayNotifyWndPtr.ToString)

        Console.WriteLine("Disable Redraw for ReBarWindow32...")
        SendMessage(ReBarWindow32Ptr, WM_SETREDRAW, False, 0)

        Console.WriteLine("Starting TaskbarCalculator MultiThread...")
        Dim t1 As System.Threading.Thread = New System.Threading.Thread(AddressOf TaskbarCalculator)
        t1.Start()

        Console.WriteLine("Starting TaskbarMover MultiThread...")
        Dim t2 As System.Threading.Thread = New System.Threading.Thread(AddressOf TaskbarMover)
        t2.Start()

    End Sub

    Sub TaskbarCalculator()
        Do

            Try


                Dim Laps As Integer


                Dim TaskbarWidth As Integer = 0
                Dim OldTaskbarCount As Integer
                Dim TaskbarCount As Integer = 0
                Dim tw As TreeWalker = TreeWalker.ControlViewWalker
                Dim child As AutomationElement = tw.GetLastChild(MSTaskListWClass)

                tw = Nothing

                TaskbarCount = child.Current.BoundingRectangle.Left

                If Not TaskbarCount = OldTaskbarCount Then
                    OldTaskbarCount = TaskbarCount
                    For Each ui As AutomationElement In MSTaskListWClass.FindAll(TreeScope.Descendants, New PropertyCondition(AutomationElement.IsControlElementProperty, True))
                        If Not ui.Current.Name = Nothing Then
                            TaskbarWidth = TaskbarWidth + ui.Current.BoundingRectangle.Width
                            System.Threading.Thread.Sleep(5)
                        End If
                    Next
                    TaskbarWidthFull = TaskbarWidth
                End If

                Dim rct As RECT
                GetWindowRect(ReBarWindow32Ptr, rct)
                TaskbarLeft = rct.Left

                Laps = Laps + 1

                If Laps = 20 Then
                    Laps = 0
                    SaveMemory()
                End If

                System.Threading.Thread.Sleep(400)

            Catch ex As Exception
                Console.WriteLine("TaskbarCalculator : " & ex.Message)
            End Try


        Loop
    End Sub

    Public Function SaveMemory() As Int32

        Return SetProcessWorkingSetSize(Diagnostics.Process.GetCurrentProcess.Handle, -1, -1)

    End Function

    Sub TaskbarMover()
        System.Threading.Thread.Sleep(500)
        Do

            Try

                System.Threading.Thread.Sleep(250)
                Dim TaskbarWidthHalf = TaskbarWidthFull / 2
                Dim position = Screen.PrimaryScreen.Bounds.Width / 2 - TaskbarWidthHalf - TaskbarLeft + NumericUpDown2.Value - 4



                TaskbarNewPos = position

                Me.Invoke(New Action(Sub()
                                         Label1.Text = position
                                     End Sub))

                If IsTaskbarMoving = False Then
                    SetWindowPos(MSTaskListWClassPtr, IntPtr.Zero, position, 0, 0, 0, SWP_NOSIZE Or SWP_ASYNCWINDOWPOS Or SWP_NOSENDCHANGING Or SWP_NOACTIVATE Or SWP_NOCOPYBITS Or SWP_NOOWNERZORDER)
                End If


            Catch ex As Exception
                Console.WriteLine("TaskbarMover : " & ex.Message)
            End Try

        Loop
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        Console.WriteLine("Saving Settings...")
        My.Settings.Save()

        Console.WriteLine("Enable Redraw for ReBarWindow32...")
        SendMessage(ReBarWindow32Ptr, WM_SETREDRAW, True, 0)

        Console.WriteLine("Setting Taskbar back to Default Position...")
        SetWindowPos(MSTaskListWClassPtr, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE Or SWP_ASYNCWINDOWPOS Or SWP_NOSENDCHANGING Or SWP_NOACTIVATE Or SWP_NOCOPYBITS Or SWP_NOOWNERZORDER)

        NotifyIcon1.Visible = False


        Console.WriteLine("Closing...")
        Me.Close()
        End
    End Sub

    Private Sub LinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
        Process.Start("https://chrisandriessen.nl")
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Console.WriteLine("Saving Settings...")
        My.Settings.Save()
        Me.Hide()
        Me.Opacity = 0
    End Sub

    Private Sub Label1_TextChanged(sender As Object, e As EventArgs) Handles Label1.TextChanged
        FluentMove()
    End Sub

    Sub FluentMove()
        Try

            IsTaskbarMoving = True

            Dim Speed As Integer = NumericUpDown1.Value

            If RadioButton2.Checked = True Then
                Dim r1 As Transition = New Transition(New TransitionType_Acceleration(Speed))
                r1.add(Panel1, "Left", TaskbarNewPos)
                r1.run()
            End If

            If RadioButton4.Checked = True Then
                Dim r3 As Transition = New Transition(New TransitionType_CriticalDamping(Speed))
                r3.add(Panel1, "Left", TaskbarNewPos)
                r3.run()
            End If

            If RadioButton3.Checked = True Then
                Dim r4 As Transition = New Transition(New TransitionType_Deceleration(Speed))
                r4.add(Panel1, "Left", TaskbarNewPos)
                r4.run()
            End If

            If RadioButton5.Checked = True Then
                Dim r5 As Transition = New Transition(New TransitionType_EaseInEaseOut(Speed))
                r5.add(Panel1, "Left", TaskbarNewPos)
                r5.run()
            End If

            If RadioButton6.Checked = True Then
                Dim r7 As Transition = New Transition(New TransitionType_Linear(Speed))
                r7.add(Panel1, "Left", TaskbarNewPos)
                r7.run()
            End If

            If RadioButton1.Checked = True Then
                Panel1.Left = TaskbarNewPos
            End If

            Do Until Panel1.Left = TaskbarNewPos
                Application.DoEvents()
            Loop

            IsTaskbarMoving = False


        Catch ex As Exception
            Console.WriteLine("FluentMove : " & ex.Message)
        End Try
    End Sub

    Private Sub Panel1_Move(sender As Object, e As EventArgs) Handles Panel1.Move
        Console.WriteLine("Disable Redraw for ReBarWindow32...")
        SendMessage(ReBarWindow32Ptr, WM_SETREDRAW, False, 0)

        Console.WriteLine("Moving Taskbar...")
        SetWindowPos(MSTaskListWClassPtr, IntPtr.Zero, Panel1.Left, 0, 0, 0, SWP_NOZORDER Or SWP_NOSIZE Or SWP_ASYNCWINDOWPOS Or SWP_NOSENDCHANGING Or SWP_NOACTIVATE)
    End Sub

    Private Sub NotifyIcon1_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles NotifyIcon1.Click
        Me.Opacity = 100
        Me.Show()
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        NotifyIcon1.Visible = False
        Application.Restart()
        End
    End Sub

    Sub RunAtStartUp()

        Dim str As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\Microsoft\Windows\Start Menu\Programs\Startup"
        File.Delete(str + "\FalconX.lnk")

        If CheckBox7.Checked = True Then
            Console.WriteLine("Creating Startup Entry...")
            Dim regKey As Microsoft.Win32.RegistryKey
            regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True)
            regKey.SetValue(Application.ProductName, """" & Application.ExecutablePath & """")
            regKey.Close()
        Else
            Console.WriteLine("Removing Startup Entry...")
            Dim regKey As Microsoft.Win32.RegistryKey
            regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True)
            regKey.DeleteValue(Application.ProductName, False)
            regKey.Close()
        End If

        Console.WriteLine("Saving Settings...")
        My.Settings.Save()

    End Sub

    Private Sub CheckBox7_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox7.CheckedChanged
        RunAtStartUp()
    End Sub


End Class
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class RichTextBoxPlus
    Inherits RichTextBox

    ''' <summary>
    ''' Controls whether TextChanged or SelectionChanged will raise
    ''' Set false to allow non-signalling property sets
    ''' </summary>
    Public EventsEnabled As Boolean = True
    Private Const WM_SETREDRAW As Integer = &HB
    Private Const WM_MOUSEACTIVATE As Integer = &H21
    Private Const WM_USER As Integer = &H400
    Private Const EM_GETEVENTMASK As Integer = WM_USER + 59
    Private Const EM_SETEVENTMASK As Integer = WM_USER + 69
    Private Const EM_GETSCROLLPOS As Integer = WM_USER + 221
    Private Const EM_SETSCROLLPOS As Integer = WM_USER + 222
    Private _ScrollPoint As Point
    Private _Painting As Boolean = True
    Private _EventMask As IntPtr
    Private _SuspendIndex As Integer = 0
    Private _SuspendLength As Integer = 0


    <DllImport("user32.dll")>
    Private Shared Function SendMessage(ByVal hWnd As IntPtr, ByVal wMsg As Int32, ByVal wParam As Int32, ByRef lParam As Point) As IntPtr
    End Function

    Protected Overrides Sub WndProc(ByRef m As Message)
        'Fixes an issue where clicking on the textbox while the form that it lives on isn't in focus
        'would only focus it, not change the selection
        If m.Msg = WM_MOUSEACTIVATE Then
            Me.Focus()
        End If

        MyBase.WndProc(m)
    End Sub

    'See https://stackoverflow.com/questions/6547193/how-to-append-text-to-richtextbox-without-scrolling-and-losing-selection

    ''' <summary>
    ''' Supress paint events and auto scrolling until EndUpdate is called
    ''' </summary>
    Public Sub BeginUpdate()
        If _Painting Then
            _SuspendIndex = Me.SelectionStart
            _SuspendLength = Me.SelectionLength
            SendMessage(Me.Handle, EM_GETSCROLLPOS, 0, _ScrollPoint)
            SendMessage(Me.Handle, WM_SETREDRAW, 0, IntPtr.Zero)
            _EventMask = SendMessage(Me.Handle, EM_GETEVENTMASK, 0, IntPtr.Zero)
            _Painting = False
        End If
    End Sub

    ''' <summary>
    ''' Resumes control painting, use after BeginUpdate
    ''' </summary>
    Public Sub EndUpdate()
        If Not _Painting Then
            Me.[Select](_SuspendIndex, _SuspendLength)
            SendMessage(Me.Handle, EM_SETSCROLLPOS, 0, _ScrollPoint)
            SendMessage(Me.Handle, EM_SETEVENTMASK, 0, _EventMask)
            SendMessage(Me.Handle, WM_SETREDRAW, 1, IntPtr.Zero)
            _Painting = True
            Me.Invalidate()
        End If
    End Sub

    ''' <summary>
    ''' Sets the text value of the control without signalling an event raise
    ''' </summary>
    Public Sub SetText(newText As String)
        EventsEnabled = False
        Dim lastSelection As Integer = Me.SelectionStart
        Dim lastSelectionLength As Integer = Me.SelectionLength
        Me.Text = newText
        Me.SelectionStart = lastSelection
        Me.SelectionLength = lastSelectionLength
        EventsEnabled = True
    End Sub

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        'Fixes a bug in RichTextbox that causes this property, even when false, to make selecting while dragging cursor select full words
        'See https://stackoverflow.com/questions/3678620/c-sharp-richtextbox-selection-problem
        If Not MyBase.AutoWordSelection Then
            MyBase.AutoWordSelection = True
            MyBase.AutoWordSelection = False
        End If
    End Sub

    Protected Overrides Sub OnTextChanged(e As EventArgs)
        If Not EventsEnabled Then
            Exit Sub
        End If
        MyBase.OnTextChanged(e)
    End Sub

    Protected Overrides Sub OnSelectionChanged(e As EventArgs)
        If Not EventsEnabled Then
            Exit Sub
        End If
        MyBase.OnSelectionChanged(e)
    End Sub

    <DllImport("user32.dll")>
    Private Shared Function SendMessage(ByVal hWnd As IntPtr, ByVal Msg As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As IntPtr
    End Function
End Class

﻿Imports System.Drawing.Imaging
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.ComTypes
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.VisualBasic.Devices
Imports Shell32

Module Extensions
    Public DeltaELimit As Integer = 5

    ''' <summary>
    ''' Checks that a given value is equal to another within a given margen of error
    ''' </summary>
    <Extension>
    Public Function EqualsWithin(value1 As Double, value2 As Double, margin As Double) As Boolean
        Return value2 <= value1 + margin AndAlso value2 >= value1 - margin
    End Function

    ''' <summary>
    ''' Checks if the given value is within the range from min to max inclusive
    ''' </summary>
    <Extension>
    Public Function InRange(value1 As Integer, min As Integer, max As Integer) As Boolean
        Return value1 >= min AndAlso value1 <= max
    End Function

    ''' <summary>
    ''' Forces the given value to the closest value between min and max (inclusive)
    ''' </summary>
    <Extension>
    Public Function Bound(value As Integer, min As Integer, max As Integer) As Integer
        Return Math.Min(Math.Max(value, min), max)
    End Function

    ''' <summary>
    ''' Fills a range of values of an array with a given value
    ''' </summary>
    <Extension>
    Public Sub FillRange(array As Double(), value As Double, start As Integer, length As Integer)
        For index As Integer = start To start + length - 1
            array(index) = value
        Next
    End Sub

    ''' <summary>
    ''' Distance from a point to the center of the given rectangle
    ''' </summary>
    <Extension>
    Public Function DistanceToCenter(rect As RectangleF, location As Point) As Single
        Dim rectCenterX As Single = (rect.X + rect.Width / 2)
        Dim rectCenterY As Single = (rect.Y + rect.Height / 2)
        Return Math.Sqrt(Math.Pow(location.X - rectCenterX, 2) + Math.Pow(location.Y - rectCenterY, 2))
    End Function

    ''' <summary>
    ''' Returns the point at the center of the rectangle
    ''' </summary>
    <Extension>
    Public Function Center(rect As RectangleF) As PointF
        Return New PointF(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2)
    End Function

    ''' <summary>
    ''' Distance from a point to the center of the given rectangle
    ''' </summary>
    <Extension>
    Public Function DistanceTo(pt1 As Point, pt2 As Point) As Single
        Return Math.Sqrt(Math.Pow(pt2.X - pt1.X, 2) + Math.Pow(pt2.Y - pt1.Y, 2))
    End Function

    ''' <summary>
    ''' Compares two byte arrays(must be of same length), and returns their average difference between bytes with a value between 0 and 1
    ''' </summary>
    <Extension>
    Public Function CompareArraysAvg(array1 As Byte(), array2 As Byte()) As Double
        If array1.Count <> array2.Count Then
            Return False
        End If

        Dim difSum As Double = 0
        For index As Integer = 0 To array1.Count - 1
            difSum += Math.Abs(CType(array1(index), Integer) - array2(index)) / 255
        Next
        Dim difAvg As Double = difSum / array1.Count
        Return difAvg
    End Function

    ''' <summary>
    ''' Compares two double arrays(must be of same length), and returns their average difference
    ''' </summary>
    <Extension>
    Public Function CompareArrays(array1 As Double(), array2 As Double()) As Double
        If array1.Count <> array2.Count Then
            Return False
        End If

        Dim difSum As Double = 0
        For index As Integer = 0 To array1.Count - 1
            difSum += Math.Abs(array1(index) - array2(index))
        Next
        Dim difAvg As Double = difSum / array1.Count
        Return difAvg
    End Function

    ''' <summary>
    ''' Converts a double like 100.5 seconds to HHMMSSm... like "00:01:40.5"
    ''' </summary>
    Public Function FormatHHMMSSm(ByVal totalSS As Decimal) As String
        Dim tempTime As New DateTime(totalSS * 10000000L)
        Dim hrString As String = tempTime.Hour.ToString.PadLeft(2, "0")
        Dim minString As String = tempTime.Minute.ToString.PadLeft(2, "0")
        Dim secString As String = tempTime.Second.ToString.PadLeft(2, "0")
        Dim milliString As String = tempTime.Millisecond.ToString().PadLeft(3, "0")
        Return $"{hrString}:{minString}:{secString}.{milliString}"
    End Function

    ''' <summary>
    ''' Converts a time like "00:01:40.5" to the total number of seconds
    ''' </summary>
    ''' <param name="duration"></param>
    ''' <returns></returns>
    Public Function HHMMSSssToSeconds(ByVal duration As String) As Double
        Dim sign As Integer = 1
        If duration.StartsWith("-") Then
            duration = duration.Substring(1)
            sign = -1
        End If
        Dim totalSeconds As Double = 0
        totalSeconds += Integer.Parse(duration.Substring(0, 2)) * 60 * 60 'Hours
        totalSeconds += Integer.Parse(duration.Substring(3, 2)) * 60 'Minutes
        totalSeconds += Integer.Parse(duration.Substring(6, 2)) 'Seconds
        totalSeconds += Integer.Parse(duration.Substring(9, 2)) / 100.0 'Milliseconds
        Return totalSeconds * sign
    End Function

    ''' <summary>
    ''' Takes a number like 123 and forces it to be divisible by 2, either by returning 122 or 124
    ''' </summary>
    Public Function ForceEven(ByVal number As Integer, Optional ByVal forceDown As Boolean = True) As String
        If number \ 2 = (number - 1) \ 2 Then
            Return number + If(forceDown, -1, 1)
        Else
            Return number
        End If
    End Function

    <DllImport("shell32.dll", EntryPoint:="SHOpenFolderAndSelectItems")>
    Private Function SHOpenFolderAndSelectItems(<[In]> pidlFolder As IntPtr, cidl As UInteger, <[In], [Optional]> apidl As IntPtr, dwFlags As Integer) As Integer
    End Function

    <DllImport("shell32.dll", CharSet:=CharSet.Unicode)>
    Public Function ILCreateFromPath(<[In], MarshalAs(UnmanagedType.LPWStr)> pszPath As String) As IntPtr
    End Function

    <DllImport("shell32.dll")>
    Public Sub ILFree(<[In]> pidl As IntPtr)
    End Sub

    ''' <summary>
    ''' Opens a file explorer window targeting the given filepath. Will use an existing explorer window of the same folder and retarget if one exists.
    ''' </summary>
    ''' <param name="filePath"></param>
    Public Sub OpenOrFocusFile(ByVal filePath As String)
        Dim idStructure As Object = ILCreateFromPath(filePath)
        If idStructure <> IntPtr.Zero Then
            SHOpenFolderAndSelectItems(idStructure, 0, 0, 0)
            ILFree(idStructure)
        End If
    End Sub

    ''' <summary>
    ''' Creates a list of ranges (Min,Max) for the given list of integers
    ''' </summary>
    <Extension>
    Public Function CreateRanges(numbers As List(Of Integer)) As List(Of List(Of Integer))
        'Sanitize frames list to be in order
        numbers.Sort()
        'Extract continuous ranges like 4-17, 21-25
        Dim ranges As New List(Of List(Of Integer))
        ranges.Add(New List(Of Integer)({numbers(0), numbers(0)})) 'Min and Max range
        Dim rangeIndex As Integer = 0
        For Each intValue In numbers
            If intValue = ranges(rangeIndex)(1) + 1 Then
                ranges(rangeIndex)(1) = intValue
            ElseIf intValue > ranges(rangeIndex)(1) + 1 Then
                ranges.Add(New List(Of Integer)({intValue, intValue}))
                rangeIndex += 1
            End If
        Next
        Return ranges
    End Function

    ''' <summary>
    ''' Get an array of bytes from the given image
    ''' </summary>
    <Extension>
    Public Function GetBytes(image As Bitmap) As Byte()
        Dim objData As BitmapData = image.LockBits(New Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat)

        Dim byteCount As Integer = objData.Stride * objData.Height
        Dim pxBytes(byteCount - 1) As Byte

        Marshal.Copy(objData.Scan0, pxBytes, 0, byteCount)

        image.UnlockBits(objData)
        Return pxBytes
    End Function

    ''' <summary>
    ''' Write the given byte array to the image
    ''' Image should be initialized to the proper size beforehand
    ''' </summary>
    <Extension>
    Public Sub SetBytes(image As Bitmap, bytes As Byte())
        Dim objData As BitmapData = image.LockBits(New Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, image.PixelFormat)

        Dim byteCount As Integer = objData.Stride * objData.Height

        Marshal.Copy(bytes, 0, objData.Scan0, byteCount)

        image.UnlockBits(objData)
    End Sub

    ''' <summary>
    ''' Loop over an image to get the bounds of the contents based on the background color
    ''' </summary>
    ''' <param name="img1"></param>
    ''' <param name="startingRectangle">Rectangle to bound based off. Bounding will be done as if the given starting rect is the outer bounds of the image.</param>
    ''' <param name="backColor"></param>
    ''' <returns></returns>
    <Extension>
    Public Function BoundContents(img1 As Bitmap, Optional startingRectangle As Rectangle? = Nothing, Optional backColor As Color? = Nothing, Optional alphaLimit As Integer = 0) As Rectangle
        Dim imageBytes() As Byte = img1.GetBytes

        Dim imageData As BitmapData = img1.LockBits(New Rectangle(0, 0, img1.Width, img1.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
        Dim stride As Integer = imageData.Stride
        img1.UnlockBits(imageData)


        Dim startingRect As Rectangle
        If startingRectangle Is Nothing Then
            startingRect = New Rectangle(0, 0, img1.Width, img1.Height)
        Else
            startingRect = startingRectangle.Value
        End If

        Dim minDeltaE As Double = Double.MaxValue
        If backColor Is Nothing Then
            'Set backcolor as most common color of the four corners
            Dim possibleColors As New List(Of Color)
            possibleColors.Add(imageBytes.GetPixel(startingRect.Left, startingRect.Top, stride, 4))
            possibleColors.Add(imageBytes.GetPixel(startingRect.Right - 1, startingRect.Top, stride, 4))
            possibleColors.Add(imageBytes.GetPixel(startingRect.Right - 1, startingRect.Bottom - 1, stride, 4))
            possibleColors.Add(imageBytes.GetPixel(startingRect.Left, startingRect.Bottom - 1, stride, 4))

            'Use cielab color space to find color that has best Delta E with the others
            For Each objColor In possibleColors
                Dim testDeltaE As Double = possibleColors.Average(Function(objColor2) objColor.CompareDeltaE(objColor2))
                If testDeltaE < minDeltaE Then
                    minDeltaE = testDeltaE
                    backColor = objColor
                End If
            Next
        End If

        Dim left As Integer = startingRect.X + startingRect.Width - 1
        Dim top As Integer = startingRect.Y + startingRect.Height - 1
        Dim right As Integer = startingRect.X
        Dim bottom As Integer = startingRect.Y
        'Look for top/bottom
        For xIndex As Integer = startingRect.X To startingRect.Right - 1
            'Top edge
            For yIndex As Integer = startingRect.Y To startingRect.Bottom - 1
                Dim pixIndex As Integer = (xIndex * 4) + yIndex * stride
                Dim srcPix As Color = imageBytes.GetPixel(xIndex, yIndex, stride, 4)
                If (backColor.Value.A = 0 AndAlso srcPix.A = 0) OrElse (backColor.Value.CompareDeltaE(srcPix) < DeltaELimit) OrElse (backColor.Value.A = 0 AndAlso srcPix.A < alphaLimit) Then
                    'If (backColor.Value.A = 0 AndAlso srcPix.A = 0) OrElse (backColor.Value.Equivalent(srcPix, PixelEquivalenceLimit)) OrElse (backColor.Value.A = 0 AndAlso srcPix.A < alphaLimit) Then
                    'Good, this is the background still
                Else
                    'We found an edge
                    top = Math.Min(top, yIndex)
                    Exit For
                End If
            Next
            'Bottom edge
            For yIndex As Integer = startingRect.Bottom - 1 To startingRect.Y Step -1
                Dim pixIndex As Integer = (xIndex * 4) + yIndex * stride
                Dim srcPix As Color = imageBytes.GetPixel(xIndex, yIndex, stride, 4)
                If (backColor.Value.A = 0 AndAlso srcPix.A = 0) OrElse (backColor.Value.CompareDeltaE(srcPix) < DeltaELimit) OrElse (backColor.Value.A = 0 AndAlso srcPix.A < alphaLimit) Then
                    'Good, this is the background still
                Else
                    'We found an edge
                    bottom = Math.Max(bottom, yIndex)
                    Exit For
                End If
            Next
        Next
        'Look for left/right
        For yIndex As Integer = startingRect.Y To startingRect.Bottom - 1
            'left edge
            For xIndex As Integer = startingRect.X To startingRect.Right - 1
                Dim pixIndex As Integer = (xIndex * 4) + yIndex * stride
                Dim srcPix As Color = imageBytes.GetPixel(xIndex, yIndex, stride, 4)
                If (backColor.Value.A = 0 AndAlso srcPix.A = 0) OrElse (backColor.Value.CompareDeltaE(srcPix) < DeltaELimit) OrElse (backColor.Value.A = 0 AndAlso srcPix.A < alphaLimit) Then
                    'Good, this is the background still
                Else
                    'We found an edge
                    left = Math.Min(left, xIndex)
                    Exit For
                End If
            Next
            'right edge
            For xIndex As Integer = startingRect.Right - 1 To startingRect.X Step -1
                Dim pixIndex As Integer = (xIndex * 4) + yIndex * stride
                Dim srcPix As Color = imageBytes.GetPixel(xIndex, yIndex, stride, 4)
                If (backColor.Value.A = 0 AndAlso srcPix.A = 0) OrElse (backColor.Value.Equivalent(srcPix, DeltaELimit)) OrElse (backColor.Value.A = 0 AndAlso srcPix.A < alphaLimit) Then
                    'Good, this is the background still
                Else
                    'We found an edge
                    right = Math.Max(right, xIndex)
                    Exit For
                End If
            Next
        Next
        Return New Rectangle(left, top, (right - left) + 1, (bottom - top) + 1)
    End Function

    ''' <summary>
    ''' Expands the bounds until the closest solid borders that encapsulate the contents
    ''' </summary>
    <Extension>
    Public Function ExpandContents(img1 As Bitmap, Optional startingRectangle As Rectangle? = Nothing, Optional alphaLimit As Integer = 0) As Rectangle
        Dim imageData As BitmapData = img1.LockBits(New Rectangle(0, 0, img1.Width, img1.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
        Dim stride As Integer = imageData.Stride
        img1.UnlockBits(imageData)

        Dim startingRect As Rectangle
        If startingRectangle Is Nothing Then
            startingRect = New Rectangle(0, 0, img1.Width, img1.Height)
        Else
            startingRect = startingRectangle
        End If
        Dim left As Integer = Math.Max(startingRect.X, 0)
        Dim top As Integer = Math.Max(startingRect.Y, 0)
        Dim right As Integer = Math.Min(startingRect.X + startingRect.Width - 1, img1.Width)
        Dim bottom As Integer = Math.Min(startingRect.Y + startingRect.Height - 1, img1.Height)
        Dim centerY As Integer = startingRect.Center.Y
        Dim centerX As Integer = startingRect.Center.X

        'Left line check
        Dim hasExpanded As Integer = 1
        Dim currentBounds As Rectangle = New Rectangle(left, top, (right - left) + 1, (bottom - top) + 1)
        While hasExpanded > 0
            hasExpanded = 0
            Dim leftExp As Integer = ExpandBoundEdge(img1, currentBounds, 0)
            If leftExp < left Then
                hasExpanded += 1
                left = leftExp
            End If
            Dim topExp As Integer = ExpandBoundEdge(img1, currentBounds, 1)
            If topExp < top Then
                hasExpanded += 1
                top = topExp
            End If
            Dim rightExp As Integer = ExpandBoundEdge(img1, currentBounds, 2)
            If rightExp > right Then
                hasExpanded += 1
                right = rightExp
            End If
            Dim bottomExp As Integer = ExpandBoundEdge(img1, currentBounds, 3)
            If bottomExp > bottom Then
                hasExpanded += 1
                bottom = bottomExp
            End If

            currentBounds = New Rectangle(left, top, (right - left) + 1, (bottom - top) + 1)
        End While

        Return currentBounds
    End Function

    ''' <summary>
    ''' Attempts to expand the given edge until the first connected line is hit
    ''' side is 0=left, 1=top, 2=right, 3=bottom
    ''' </summary>
    ''' <returns></returns>
    Private Function ExpandBoundEdge(img1 As Bitmap, boundRect As Rectangle, side As Integer) As Integer
        Dim imageBytes() As Byte = img1.GetBytes

        Dim imageData As BitmapData = img1.LockBits(New Rectangle(0, 0, img1.Width, img1.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
        Dim stride As Integer = imageData.Stride
        img1.UnlockBits(imageData)

        Dim imageRect As Rectangle = img1.Size.ToRect
        Dim startIndex As Integer = 0
        Dim endIndex As Integer = 0
        Dim boundCenter As Point = boundRect.Center
        Dim isHorizontal As Boolean = False
        Dim startPerp As Integer = 0
        Dim endPerp1 As Integer = 0
        Dim endPerp2 As Integer = 0
        Select Case side
            Case 0 'Left
                startIndex = boundRect.Left
                endIndex = 0
            Case 1 'Top
                startIndex = boundRect.Top
                endIndex = 0
            Case 2 'Right
                startIndex = boundRect.Right - 1
                endIndex = img1.Width - 1
            Case 3 'Bottom
                startIndex = boundRect.Bottom - 1
                endIndex = img1.Height - 1
        End Select

        Select Case side
            Case 0, 2 'Left,right
                startPerp = boundCenter.Y
                endPerp1 = boundRect.Top
                endPerp2 = boundRect.Bottom - 1
                isHorizontal = True
            Case 1, 3 'Top,bottom
                startPerp = boundCenter.X
                endPerp1 = boundRect.Left
                endPerp2 = boundRect.Right - 1
                isHorizontal = False
        End Select

        Dim stepDirection As Integer = (If(startIndex > endIndex, -1, 1))
        For index As Integer = startIndex + stepDirection To endIndex Step stepDirection
            Dim xIndex As Integer = If(isHorizontal, index, startPerp)
            Dim yIndex As Integer = If(isHorizontal, startPerp, index)
            Dim pixIndex As Integer = (xIndex * 4) + yIndex * stride
            If Not imageRect.Contains(xIndex, yIndex) Then
                'Don't check outside the bounds of the image
                Return startIndex
            End If
            Dim startPixel As Color = imageBytes.GetPixel(xIndex, yIndex, stride, 4)
            Dim areEquivalent As Boolean = True
            'Expand perpendicular
            For perpIndex As Integer = startPerp To endPerp1 Step -1
                xIndex = If(isHorizontal, index, perpIndex)
                yIndex = If(isHorizontal, perpIndex, index)
                pixIndex = (xIndex * 4) + yIndex * stride
                If Not imageRect.Contains(xIndex, yIndex) Then
                    'Don't check outside the bounds of the image
                    Continue For
                End If
                Dim srcPix As Color = imageBytes.GetPixel(xIndex, yIndex, stride, 4)
                If Not srcPix.CompareDeltaE(startPixel) < DeltaELimit Then
                    areEquivalent = False
                    Exit For
                End If
            Next
            If Not areEquivalent Then
                'Reached edge of check without a consistent bound, should be outer edge of image
                If index = endIndex Then
                    Return index
                End If
                Continue For
            End If
            'Expand perpendicular the other way
            For perpIndex As Integer = startPerp To endPerp2
                xIndex = If(isHorizontal, index, perpIndex)
                yIndex = If(isHorizontal, perpIndex, index)
                pixIndex = (xIndex * 4) + yIndex * stride
                If Not imageRect.Contains(xIndex, yIndex) Then
                    'Don't check outside the bounds of the image
                    Continue For
                End If
                Dim srcPix As Color = imageBytes.GetPixel(xIndex, yIndex, stride, 4)
                If Not srcPix.CompareDeltaE(startPixel) < DeltaELimit Then
                    areEquivalent = False
                    Exit For
                End If
            Next
            If Not areEquivalent Then
                'Reached edge of check without a consistent bound, should be outer edge of image
                If index = endIndex Then
                    Return index
                End If
            End If
            If areEquivalent Then
                Return index - stepDirection
            End If
        Next

        Return startIndex
    End Function


    '''' <summary>
    '''' Converts a point in control coordinates to image coordinates
    '''' </summary>
    <Extension>
    Public Function PointToImage(picControl As PictureBox, controlPoint As Point, Optional realSize As Size = Nothing) As Point
        If picControl.Image Is Nothing Then
            Return New Point(0, 0)
        End If
        Dim displaySize As Size = picControl.Image.Size
        If realSize.Width > 0 AndAlso realSize.Height > 0 Then
            displaySize = realSize
        End If
        'Calculate actual crop locations due to bars and aspect ratio changes
        Dim actualAspectRatio As Double = (displaySize.Height / displaySize.Width)
        Dim picVideoAspectRatio As Double = (picControl.Height / picControl.Width)
        Dim fitRatio As Double = Math.Min(picControl.Height / displaySize.Height, picControl.Width / displaySize.Width)
        Dim verticalBarSizeRealPx As Integer = If(actualAspectRatio < picVideoAspectRatio, (picControl.Height - displaySize.Height * fitRatio) / 2, 0)
        Dim horizontalBarSizeRealPx As Integer = If(actualAspectRatio > picVideoAspectRatio, (picControl.Width - displaySize.Width * fitRatio) / 2, 0)
        Return New Point(((controlPoint.X - horizontalBarSizeRealPx) / fitRatio), ((controlPoint.Y - verticalBarSizeRealPx) / fitRatio)).Bound(New Rectangle(New Point(0, 0), displaySize))
    End Function

    '''' <summary>
    '''' Converts a point in image coordinates to control coordinates
    '''' </summary>
    <Extension>
    Public Function ImagePointToClient(picControl As PictureBox, controlPoint As Point, Optional realSize As Size = Nothing) As Point
        If picControl.Image Is Nothing Then
            Return New Point(0, 0)
        End If
        Dim displaySize As Size = picControl.Image.Size
        If realSize.Width > 0 AndAlso realSize.Height > 0 Then
            displaySize = realSize
        End If
        'Calculate actual crop locations due to bars and aspect ratio changes
        Dim actualAspectRatio As Double = (displaySize.Height / displaySize.Width)
        Dim picVideoAspectRatio As Double = (picControl.Height / picControl.Width)
        Dim fitRatio As Double = Math.Min(picControl.Height / displaySize.Height, picControl.Width / displaySize.Width)
        Dim verticalBarSizeRealPx As Integer = If(actualAspectRatio < picVideoAspectRatio, (picControl.Height - displaySize.Height * fitRatio) / 2, 0)
        Dim horizontalBarSizeRealPx As Integer = If(actualAspectRatio > picVideoAspectRatio, (picControl.Width - displaySize.Width * fitRatio) / 2, 0)
        Return New Point(Math.Max(0, (controlPoint.X * fitRatio + horizontalBarSizeRealPx)), Math.Max(0, (controlPoint.Y * fitRatio + verticalBarSizeRealPx)))
    End Function


    <Extension>
    Public Function ContentToClient(picControl As PictureBox, pt1 As Point, realSize As Size) As Point
        Dim displaySize As Size = realSize
        Dim actualAspectRatio As Double = (displaySize.Height / displaySize.Width)
        Dim picVideoAspectRatio As Double = (picControl.Height / picControl.Width)
        Dim fitRatio As Double = Math.Min(picControl.Height / displaySize.Height, picControl.Width / displaySize.Width)
        Dim verticalBarSizeRealPx As Integer = If(actualAspectRatio < picVideoAspectRatio, (picControl.Height - displaySize.Height * fitRatio) / 2, 0)
        Dim horizontalBarSizeRealPx As Integer = If(actualAspectRatio > picVideoAspectRatio, (picControl.Width - displaySize.Width * fitRatio) / 2, 0)
        Dim displayHeight As Double = picControl.Height - (verticalBarSizeRealPx * 2)
        Dim displayWidth As Double = picControl.Width - (horizontalBarSizeRealPx * 2)

        Return New Point(Math.Max(0, (pt1.X / displaySize.Width) * displayWidth + horizontalBarSizeRealPx), Math.Max(0, (pt1.Y / displaySize.Height) * displayHeight + verticalBarSizeRealPx))
    End Function

    ''' <summary>
    ''' Ensures the given point falls within the rectangle
    ''' </summary>
    ''' <param name="pt"></param>
    ''' <param name="rect"></param>
    ''' <returns></returns>
    <Extension>
    Public Function Bound(pt As Point, rect As Rectangle) As Point
        Return New Point(Math.Min(Math.Max(rect.Left, pt.X), rect.Right - 1), Math.Min(Math.Max(rect.Top, pt.Y), rect.Bottom - 1))
    End Function

    ''' <summary>
    ''' Gets the coordinate of the top left of the rectangle
    ''' </summary>
    <Extension>
    Public Function TopLeft(rect As Rectangle) As Point
        Return New Point(rect.Left, rect.Top)
    End Function

    ''' <summary>
    ''' Gets the coordinate of the bottom right of the rectangle
    ''' </summary>
    <Extension>
    Public Function BottomRight(rect As Rectangle) As Point
        Return New Point(rect.Right, rect.Bottom)
    End Function

    ''' <summary>
    ''' Checks if the extension of the path is that of an image such as .png, .bmp, .jpg, .jpeg
    ''' </summary>
    <Extension>
    Public Function IsVBImage(path As String) As Boolean
        Select Case IO.Path.GetExtension(path).ToLower()
            Case ".png", ".jpg", ".bmp", ".jpeg"
                Return True
            Case Else
                Return False
        End Select
    End Function

    ''' <summary>
    ''' Center point of a rectangle
    ''' </summary>
    <Extension>
    Public Function Center(rect As Rectangle) As Point
        Return New Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2)
    End Function

    ''' <summary>
    ''' Converts point to pointf
    ''' </summary>
    <Extension>
    Public Function ToPointF(pt As Point) As PointF
        Return New PointF(pt.X, pt.Y)
    End Function

    ''' <summary>
    ''' Converts pointf to point with optional truncation
    ''' </summary>
    <Extension>
    Public Function ToPoint(pt As PointF, Optional rounding As Integer = 0) As Point
        Select Case rounding
            Case -1
                Return New Point(Math.Floor(pt.X), Math.Floor(pt.Y))
            Case 0
                Return New Point(pt.X, pt.Y)
            Case 1
                Return New Point(Math.Ceiling(pt.X), Math.Ceiling(pt.Y))
        End Select
    End Function

    ''' <summary>
    ''' Adds a vector to this point, returning the result
    ''' </summary>
    <Extension>
    Public Function Add(pt1 As Point, pt2 As Point) As Point
        Return New Point(pt1.X + pt2.X, pt1.Y + pt2.Y)
    End Function

    ''' <summary>
    ''' Subtracts a vector from this point, returning the result
    ''' </summary>
    <Extension>
    Public Function Subtract(pt1 As Point, pt2 As Point) As Point
        Return New Point(pt1.X - pt2.X, pt1.Y - pt2.Y)
    End Function

    ''' <summary>
    ''' The magnituded of a point as a vector, basic pythagorean theorem
    ''' </summary>
    <Extension>
    Public Function Magnitude(pt1 As Point) As Double
        Return Math.Sqrt(Math.Pow(pt1.X, 2) + Math.Pow(pt1.Y, 2))
    End Function

    ''' <summary>
    ''' The magnituded of a point as a vector, basic pythagorean theorem
    ''' </summary>
    <Extension>
    Public Function Magnitude(pt1 As PointF) As Double
        Return Math.Sqrt(Math.Pow(pt1.X, 2) + Math.Pow(pt1.Y, 2))
    End Function

    ''' <summary>
    ''' Multiplies a point as a vector given a scaler value
    ''' </summary>
    <Extension>
    Public Function Scale(pt1 As Point, scaler As Double) As Point
        Return New Point(pt1.X * scaler, pt1.Y * scaler)
    End Function


    ''' <summary>
    ''' Multiplies a point as a vector given a scaler value
    ''' </summary>
    <Extension>
    Public Function Scale(pt1 As PointF, scaler As Double) As Point
        Return New Point(pt1.X * scaler, pt1.Y * scaler)
    End Function

    ''' <summary>
    ''' Returns this point transformed by the given matrix
    ''' </summary>
    <Extension>
    Public Function Transform(pt1 As Point, m As System.Drawing.Drawing2D.Matrix) As Point
        Dim resultPoints As Point() = {pt1}
        m.TransformPoints(resultPoints)
        Return resultPoints(0)
    End Function

    ''' <summary>
    ''' Returns this pointf transformed by the given matrix
    ''' </summary>
    <Extension>
    Public Function Transform(pt1 As PointF, m As System.Drawing.Drawing2D.Matrix) As PointF
        Dim resultPoints As PointF() = {pt1}
        m.TransformPoints(resultPoints)
        Return resultPoints(0)
    End Function

    ''' <summary>
    ''' Checks if two colors have the same color values within a range
    ''' Limit is per channel how far off the values can be from eachother
    ''' 0 means they must be the exact same values
    ''' </summary>
    <Extension>
    Public Function Equivalent(color1 As Color, color2 As Color, Optional differenceLimit As Integer = 0) As Boolean
        Dim alphaDif As Integer = Math.Abs(CInt(color1.A) - color2.A)
        Dim redDif As Integer = Math.Abs(CInt(color1.R) - color2.R)
        Dim greenDif As Integer = Math.Abs(CInt(color1.G) - color2.G)
        Dim blueDif As Integer = Math.Abs(CInt(color1.B) - color2.B)
        Return alphaDif <= differenceLimit AndAlso redDif <= differenceLimit AndAlso greenDif <= differenceLimit AndAlso blueDif <= differenceLimit
    End Function

    Public Class ColorLAB
        Public L As Double
        Public A As Double
        Public B As Double
        Public Sub New(l, a, b)
            Me.L = l
            Me.A = a
            Me.B = b
        End Sub
        Public Overrides Function ToString() As String
            Return $"L {L}, a {A}, b {B}"
        End Function
    End Class

    Public Class ColorXYZ
        Public Shared o As Double = 6 / 29
        Public Shared o3 As Double = 216 / 24389 'o cubed
        Public X As Double
        Public Y As Double
        Public Z As Double
        Public Sub New(x, y, z)
            Me.X = x
            Me.Y = y
            Me.Z = z
        End Sub

        Public Function ToLAB() As ColorLAB
            'https://en.wikipedia.org/wiki/CIELAB_color_space#From_CIEXYZ_to_CIELAB
            'Standard illuminant D65
            Dim xr As Double = X / 0.950489
            Dim yr As Double = Y / 1.0
            Dim zr As Double = Z / 1.08884

            Dim fx As Double = LabFunction(xr)
            Dim fy As Double = LabFunction(yr)
            Dim fz As Double = LabFunction(zr)

            Dim L As Double = 116 * fy - 16
            Dim A As Double = 500 * (fx - fy)
            Dim B As Double = 200 * (fy - fz)
            Return New ColorLAB(L, A, B)
        End Function
        Private Function LabFunction(value As Double)
            If value > o3 Then
                Return Math.Pow(value, 1 / 3)
            Else
                Return (value * Math.Pow(o, -2)) / 3 + 4 / 29
            End If
        End Function
        Public Overrides Function ToString() As String
            Return $"X {X}, Y {Y}, Z {Z}"
        End Function
    End Class

    ''' <summary>
    ''' Converts an sRGB color to XYZ color space
    ''' </summary>
    <Extension>
    Public Function ToXYZ(colorRGB As Color) As ColorXYZ
        'https://www.image-engineering.de/library/technotes/958-how-to-convert-between-srgb-and-ciexyz
        'Change range of colors from 0-255 to 0-1
        Dim r As Double = colorRGB.R / 255
        Dim g As Double = colorRGB.G / 255
        Dim b As Double = colorRGB.B / 255
        r = LinearizeSRGBChannel(r)
        g = LinearizeSRGBChannel(g)
        b = LinearizeSRGBChannel(b)

        Dim x = 0.4124564 * r + 0.3575761 * g + 0.1804375 * b
        Dim y = 0.2126729 * r + 0.7151522 * g + 0.072175 * b
        Dim z = 0.0193339 * r + 0.119192 * g + 0.9503041 * b

        Return New ColorXYZ(x, y, z)
    End Function

    ''' <summary>
    ''' Compares two colors using the CIELAB color space, returning the Delta E
    ''' This is to give a more human vision based difference measurement
    ''' Alpha will cause an adjustment, so that two of the same color, one with alpha = 0, and one alpha = 255 will have Delta E of 100
    ''' </summary>
    <Extension>
    Public Function CompareDeltaE(color1 As Color, color2 As Color) As Double
        'Pre-apply alpha to move dimmed colors closer together
        Dim color1Transparency As Double = (color1.A / 255) * 100
        Dim color2Transparency As Double = (color2.A / 255) * 100
        Dim lab1 As ColorLAB = color1.ToXYZ.ToLAB
        Dim lab2 As ColorLAB = color2.ToXYZ.ToLAB
        Return Math.Sqrt((lab2.L - lab1.L) ^ 2 + (lab2.A - lab1.A) ^ 2 + (lab2.B - lab1.B) ^ 2 + (color2Transparency - color1Transparency) ^ 2)
    End Function


    ''' <summary>
    ''' Takes a color channel value 0-1 and returns the linearized value
    ''' </summary>
    Private Function LinearizeSRGBChannel(channelValue As Double)
        If channelValue < 0.04045 Then
            Return channelValue / 12.92
        Else
            Return ((channelValue + 0.055) / 1.055) ^ 2.4
        End If
    End Function


    ''' <summary>
    ''' Compares two color values
    ''' Returns less than 0 if the given color grayscale intensity is higher than the current
    ''' Returns 0 if they are the same grayscale intensity
    ''' Returns greater than 0 if the given color grayscale intensity is lower than the current
    ''' Allowance can be defined to say that two colors are the same as long as their intensity is within the allowance, with 0 meaning they must be exactly the same.
    ''' </summary>
    <Extension>
    Public Function CompareTo(color1 As Color, color2 As Color, Optional allowance As Integer = 0) As Integer
        If Math.Abs(CType(color1.Intensity, Integer) - color2.Intensity) <= allowance Then
            Return 0
        End If
        Return color1.Intensity.CompareTo(color2.Intensity)
    End Function

    ''' <summary>
    ''' Gets the color as grayscale intensity, or average channel value * alpha/255
    ''' </summary>
    <Extension>
    Public Function Intensity(color As Color) As Byte
        Return ((CType(color.R, Integer) + color.G + color.B) / 3) * (color.A / 255.0)
    End Function

    ''' <summary>
    ''' Area of a rectangle W * H
    ''' </summary>
    <Extension>
    Public Function Area(rect As Rectangle) As Integer
        If rect.Width <= 0 OrElse rect.Height <= 0 Then
            Return 0
        Else
            Return rect.Width * rect.Height
        End If
    End Function

    ''' <summary>
    ''' Area of a rectangle W * H
    ''' </summary>
    <Extension>
    Public Function Area(rect As RectangleF) As Single
        If rect.Width <= 0 OrElse rect.Height <= 0 Then
            Return 0
        Else
            Return rect.Width * rect.Height
        End If
    End Function

    ''' <summary>
    ''' returns a new rectangle scaled by the given amount towards the origin 0,0
    ''' </summary>
    <Extension>
    Public Function Scale(rect As Rectangle, scaler As Double) As Rectangle
        Return New Rectangle(rect.X * scaler, rect.Y * scaler, rect.Width * scaler, rect.Height * scaler)
    End Function

    ''' <summary>
    ''' Gets the scale required to fit this rectangle into another
    ''' </summary>
    <Extension>
    Public Function FitScale(thisRect As Rectangle, outerRect As Rectangle) As Double
        Return Math.Min(outerRect.Width / thisRect.Width, outerRect.Height / thisRect.Height)
    End Function

    ''' <summary>
    ''' Gets the scale required to fit this size as a rectangle into another
    ''' </summary>
    <Extension>
    Public Function FitScale(thisRect As Size, outerRect As Size) As Double
        Return Math.Min(outerRect.Width / thisRect.Width, outerRect.Height / thisRect.Height)
    End Function

    ''' <summary>
    ''' Converts a size to a basic rectangle of its width & height
    ''' </summary>
    <Extension>
    Public Function ToRect(size1 As Size) As Rectangle
        Return New Rectangle(0, 0, size1.Width, size1.Height)
    End Function

    ''' <summary>
    ''' Converts a sizef to a basic rectanglef of its width & height
    ''' </summary>
    <Extension>
    Public Function ToRect(size1 As SizeF) As RectangleF
        Return New RectangleF(0, 0, size1.Width, size1.Height)
    End Function

    ''' <summary>
    ''' Gets the index of the first separator in the menu strip
    ''' </summary>
    ''' <param name="menuStrip"></param>
    ''' <returns></returns>
    <Extension>
    Public Function FirstSeparator(menuStrip As ContextMenuStrip) As Integer
        For index As Integer = 0 To menuStrip.Items.Count - 1
            If menuStrip.Items(index).GetType Is GetType(ToolStripSeparator) Then
                Return index
            End If
        Next
        Return -1
    End Function

    Private Declare Unicode Function StrCmpLogicalW Lib "shlwapi.dll" (ByVal string1 As String, ByVal string2 As String) As Integer

    ''' <summary>
    ''' Uses StrCmpLogicalW to compare two strings. This uses more natural sorting order, so 1 is followed by 2, not 10 or 11
    ''' </summary>
    <Extension>
    Public Function CompareNatural(string1 As String, string2 As String) As Integer
        Return StrCmpLogicalW(string1, string2)
    End Function

    ''' <summary>
    ''' Gets a color value for a pixel out of a byte() with a known stride
    ''' </summary>
    <Extension>
    Public Function GetPixel(pixBytes As Byte(), x As Integer, y As Integer, stride As Integer, bytesPerPixel As Integer) As Color
        Dim pixIndex As Integer = (x * bytesPerPixel) + y * stride
        Return Color.FromArgb(pixBytes(pixIndex + 3), pixBytes(pixIndex + 2), pixBytes(pixIndex + 1), pixBytes(pixIndex))
    End Function

    ''' <summary>
    ''' Returns a new list of objects where no two items should compare with a result of 0
    ''' </summary>
    <Extension()>
    Public Function Distinct(Of T)(collection As List(Of T), comparer As Comparison(Of T)) As List(Of T)
        Dim tempCollection As List(Of T) = collection.ToArray.ToList
        tempCollection.Sort(comparer)
        Dim distinctCollection As New List(Of T)
        If tempCollection.Count = 0 Then
            Return distinctCollection
        End If
        distinctCollection.Add(tempCollection(0))
        Dim lastItem As T = tempCollection(0)
        For index As Integer = 1 To collection.Count - 1
            If comparer.Invoke(tempCollection(index), lastItem) = 0 Then
                'Skip same items
            Else
                distinctCollection.Add(tempCollection(index))
                lastItem = tempCollection(index)
            End If
        Next
        Return distinctCollection
    End Function

    ''' <summary>
    ''' Flattens a string collection into a delimited list
    ''' </summary>
    <Extension()>
    Public Function Flatten(collection As IEnumerable(Of String), Optional delimiter As String = ",") As String
        Dim builder As New System.Text.StringBuilder()
        For index As Integer = 0 To collection.Count - 1
            builder.Append(collection(index))
            If index <> collection.Count - 1 Then
                builder.Append(delimiter)
            End If
        Next
        Return builder.ToString
    End Function

    <Extension()>
    Public Function WaitForExitAsync(process As Process, Optional cancellationToken As CancellationToken = Nothing) As Task
        If process.HasExited Then
            Return Task.CompletedTask
        End If
        Dim completionSource As New TaskCompletionSource(Of Object)
        process.EnableRaisingEvents = True
        AddHandler process.Exited, (Sub(sender, args)
                                        completionSource.TrySetResult(Nothing)
                                    End Sub)
        If cancellationToken <> Nothing Then
            cancellationToken.Register(Sub() completionSource.SetCanceled())
        End If
        Return If(process.HasExited, Task.CompletedTask, completionSource.Task)
    End Function

#Region "Filters"
    ''' <summary>
    ''' Returns a copy of the image that has had a grayscale filter applied
    ''' </summary>
    <Extension>
    Public Function Grayscale(img As Bitmap) As Bitmap
        Dim imgBytes() As Byte = img.GetBytes

        Dim imageData As BitmapData = img.LockBits(New Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
        Dim stride As Integer = imageData.Stride
        img.UnlockBits(imageData)

        Dim filteredImage As Bitmap = img.Clone
        For pxIndex As Integer = 0 To imgBytes.Count - 1 Step 4
            Dim alpha As Byte = imgBytes(pxIndex + 3)
            Dim red As Byte = imgBytes(pxIndex + 2)
            Dim green As Byte = imgBytes(pxIndex + 1)
            Dim blue As Byte = imgBytes(pxIndex)

            'Do filter
            Dim average As Byte = CByte((CInt(red) + blue + green) / 3)
            imgBytes(pxIndex) = average
            imgBytes(pxIndex + 1) = average
            imgBytes(pxIndex + 2) = average
        Next
        filteredImage.SetBytes(imgBytes)
        Return filteredImage
    End Function

    ''' <summary>
    ''' Returns a copy of the image that has had each pixel grayscale value checked to be in the given min-max range inclusive
    ''' </summary>
    <Extension>
    Public Function Threshold(img As Bitmap, min As Byte, max As Byte) As Bitmap
        Dim imgBytes() As Byte = img.GetBytes

        Dim imageData As BitmapData = img.LockBits(New Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
        Dim stride As Integer = imageData.Stride
        img.UnlockBits(imageData)

        Dim filteredImage As Bitmap = img.Clone
        For pxIndex As Integer = 0 To imgBytes.Count - 1 Step 4
            Dim alpha As Byte = imgBytes(pxIndex + 3)
            Dim red As Byte = imgBytes(pxIndex + 2)
            Dim green As Byte = imgBytes(pxIndex + 1)
            Dim blue As Byte = imgBytes(pxIndex)

            'Do filter
            Dim average As Byte = CByte((CInt(red) + blue + green) / 3)
            If average >= min AndAlso average <= max Then
                average = 255
            Else
                average = 0
            End If
            imgBytes(pxIndex) = average
            imgBytes(pxIndex + 1) = average
            imgBytes(pxIndex + 2) = average
        Next
        filteredImage.SetBytes(imgBytes)
        Return filteredImage
    End Function
#End Region

    ''' <summary>
    ''' Gets a pattern like "image_%03d.png" from a filename like "image_001.png"
    ''' </summary>
    <Extension()>
    Public Function ExtractPattern(fileName As String) As String
        'Try to extract the constant data between the file names by removing numbers
        Dim numberRegex As New Text.RegularExpressions.Regex("(?<zeros>0+)*\d+")
        'Check padding intensity
        Dim firstMatch As Match = numberRegex.Match(fileName)
        Dim leadingZeros As Integer = firstMatch.Groups("zeros").Length
        Dim pattern As String = ""
        If leadingZeros = 0 Then
            pattern = "%d"
        ElseIf leadingZeros > 0 Then
            pattern = "%0" & leadingZeros + 1 & "d"
        Else
            Return Nothing
        End If
        Dim directoryPath As String = System.IO.Path.GetDirectoryName(fileName)
        Dim patternedName As String = System.IO.Path.GetFileName(fileName)
        patternedName = numberRegex.Replace(patternedName, pattern)
        Return System.IO.Path.Combine(directoryPath, patternedName)
    End Function

    Private Const KEY_TOGGLED As Integer = &H1
    Private Const KEY_PRESSED As Integer = &H8000

    ''' <summary>
    ''' Returns whether or not the given key is currently pressed
    ''' </summary>
    <Extension()>
    Public Function KeyPressed(keyboard As Keyboard, keycode As Keys) As Boolean
        Return GetKeyState(keycode) And KEY_PRESSED
    End Function

    ''' <summary>
    ''' Returns whether or not the given key is currently toggled
    ''' </summary>
    <Extension()>
    Public Function KeyToggled(keyboard As Keyboard, keycode As Keys) As Boolean
        Return GetKeyState(keycode) & KEY_TOGGLED
    End Function

    ''' <summary>
    ''' Allows a process to finish running, and push all of its stderror and stdoutput messages through relevant events as normally WaitForExitAsync may occur before messages are sent
    ''' https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexit?view=net-8.0&redirectedfrom=MSDN#System_Diagnostics_Process_WaitForExit_System_Int32_
    ''' </summary>
    <Extension()>
    Public Function WaitForFinishAsync(objProcess As Process) As Task(Of Boolean)
        Return Task.Run(Function()
                            If objProcess.WaitForExit(Integer.MaxValue) Then
                                objProcess.WaitForExit()
                                Return True
                            Else
                                Return False
                            End If
                        End Function)
    End Function

    ''' <summary>
    ''' Use on an async method that is not awaited in order to suppress compiler warnings
    ''' </summary>
    <Extension()>
    Public Sub Awaitnt(task As Task)
    End Sub

    ''' <summary>
    ''' Alpha blends two images by overlaying the given image on this image
    ''' See https://en.wikipedia.org/wiki/Alpha_compositing
    ''' </summary>
    ''' <param name="topImage"></param>
    <Extension()>
    Public Sub AlphaBlend(bottomImage As Bitmap, topImage As Bitmap)
        'Add onto overlaid output

        'Must manually overlay, as graphics drawimage causes edge artifacts, and poor color (even with highquality composting)
        Dim pixBytesA As Byte() = topImage.GetBytes
        Dim pixBytesB As Byte() = bottomImage.GetBytes
        'Dim pixTestColor As Color = pixBytes.GetPixel(404, 466, 2800, 4)
        For index As Integer = 0 To pixBytesA.Count - 1 Step 4
            Dim pixIndex As Integer = index
            'A
            Dim alphaA As Single = pixBytesA(pixIndex + 3) / 255
            Dim alphaB As Single = pixBytesB(pixIndex + 3) / 255
            Dim resultAlpha As Byte = Math.Min(pixBytesA(pixIndex + 3) + pixBytesB(pixIndex + 3) * (1 - alphaA), 255)
            Dim resultA As Single = resultAlpha / 255
            If resultA = 0 Then
                pixBytesA(pixIndex) = 0
                pixIndex += 1
                pixBytesA(pixIndex) = 0
                pixIndex += 1
                pixBytesA(pixIndex) = 0
                pixIndex += 1
                pixBytesA(pixIndex) = 0
            Else
                'B
                pixBytesA(pixIndex) = Math.Min((pixBytesA(pixIndex) * alphaA + pixBytesB(pixIndex) * alphaB * (1 - alphaA)) / resultA, 255)
                'G
                pixIndex += 1
                pixBytesA(pixIndex) = Math.Min((pixBytesA(pixIndex) * alphaA + pixBytesB(pixIndex) * alphaB * (1 - alphaA)) / resultA, 255)
                'R
                pixIndex += 1
                pixBytesA(pixIndex) = Math.Min((pixBytesA(pixIndex) * alphaA + pixBytesB(pixIndex) * alphaB * (1 - alphaA)) / resultA, 255)
                'A
                pixIndex += 1
                pixBytesA(pixIndex) = resultAlpha
            End If
        Next
        bottomImage.SetBytes(pixBytesA)
    End Sub

    ''' <summary>
    ''' Working implementation of GetLineFromCharIndex, but not ruined by word wrap
    ''' </summary>
    <Extension()>
    Public Function GetLineFromCharIndexUnwrapped(textBox As RichTextBox, index As Integer) As Integer
        Dim remainingDistance As Integer = index
        'Lines are split with either lf or crlf
        Dim lineMatches As MatchCollection = Regex.Matches(textBox.Text.Substring(0, index), "\n")

        Return lineMatches.Count
    End Function

    ''' <summary>
    ''' Working implementation of GetFirstCharIndexOfCurrentLine, but not ruined by word wrap
    ''' </summary>
    <Extension()>
    Public Function GetFirstCharIndexOfCurrentLineUnwrapped(textBox As RichTextBox) As String
        Dim lineMatches As MatchCollection = Regex.Matches(textBox.Text.Substring(0, textBox.SelectionStart), "\n")
        If lineMatches.Count = 0 Then
            Return 0
        End If
        Return lineMatches.Item(lineMatches.Count - 1).Groups(0).Index + 1
    End Function

    ''' <summary>
    ''' Checks the image for any pixels with alpha under 255
    ''' </summary>
    <Extension()>
    Public Function HasAlpha(objImage As Bitmap)
        Dim pixBytes As Byte() = objImage.GetBytes
        Dim imageData As BitmapData = objImage.LockBits(New Rectangle(0, 0, objImage.Width, objImage.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
        Dim stride As Integer = imageData.Stride
        objImage.UnlockBits(imageData)
        For indexX As Integer = 0 To imageData.Width - 1
            For indexY As Integer = 0 To imageData.Height - 1
                If pixBytes.GetPixel(indexX, indexY, stride, 4).A < 255 Then
                    Return True
                End If
            Next
        Next
        Return False
    End Function

    ''' <summary>
    ''' Returns byte array of ARGB color averages of the given image
    ''' </summary>
    <Extension()>
    Public Function AverageColor(objImage As Bitmap) As Color
        Dim mbytpixels As Byte() = objImage.GetBytes
        Dim pxTotal As Integer = mbytpixels.Count
        Dim result As Byte() = New Byte(3) {}
        Dim aSum As Integer = 0
        Dim rSum As Integer = 0
        Dim gSum As Integer = 0
        Dim bSum As Integer = 0
        For index As Integer = 0 To mbytpixels.Count - 1 Step 4
            bSum += mbytpixels(index)
            gSum += mbytpixels(index + 1)
            rSum += mbytpixels(index + 2)
            aSum += mbytpixels(index + 3)
        Next
        result(0) = aSum / pxTotal
        result(1) = rSum / pxTotal
        result(2) = gSum / pxTotal
        result(3) = bSum / pxTotal
        Return Color.FromArgb(result(0), result(1), result(2), result(3))
    End Function

    ''' <summary>
    ''' Calculates standard deviation of each color component in the image
    ''' Assumes BGRA pixel order
    ''' Returns ARGB order StdDev per component
    ''' </summary>
    <Extension()>
    Public Function StdDev(objImage As Bitmap, Optional ByRef averageByes As Color? = Nothing) As Double()
        Dim mbytpixels As Byte() = objImage.GetBytes
        Dim pxTotal As Integer = mbytpixels.Count / 4
        Dim result As Double() = New Double(3) {}
        Dim aSum As Integer = 0
        Dim rSum As Integer = 0
        Dim gSum As Integer = 0
        Dim bSum As Integer = 0
        For index As Integer = 0 To mbytpixels.Count - 1 Step 4
            bSum += mbytpixels(index)
            gSum += mbytpixels(index + 1)
            rSum += mbytpixels(index + 2)
            aSum += mbytpixels(index + 3)
        Next
        Dim aAvg As Double = aSum / pxTotal
        Dim rAvg As Double = rSum / pxTotal
        Dim gAvg As Double = gSum / pxTotal
        Dim bAvg As Double = bSum / pxTotal

        'We already calculated it so we might as well save the computation for later

        If averageByes Is Nothing Then
            averageByes = Color.FromArgb(aAvg, rAvg, gAvg, bAvg)
        End If

        Dim aSqr As Double = 0
        Dim rSqr As Double = 0
        Dim gSqr As Double = 0
        Dim bSqr As Double = 0
        For index As Integer = 0 To mbytpixels.Count - 1 Step 4
            bSqr += Math.Pow(mbytpixels(index) - aAvg, 2)
            gSqr += Math.Pow(mbytpixels(index + 1) - rAvg, 2)
            rSqr += Math.Pow(mbytpixels(index + 2) - gAvg, 2)
            aSqr += Math.Pow(mbytpixels(index + 3) - bAvg, 2)
        Next
        result(0) = Math.Sqrt(aSqr / pxTotal)
        result(1) = Math.Sqrt(rSqr / pxTotal)
        result(2) = Math.Sqrt(gSqr / pxTotal)
        result(3) = Math.Sqrt(bSqr / pxTotal)

        Return result
    End Function
End Module

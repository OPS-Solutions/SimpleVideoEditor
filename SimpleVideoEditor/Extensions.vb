Imports System.Drawing.Imaging
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.VisualBasic.Devices
Imports Shell32

Module Extensions
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
        Dim hours As Double = ((totalSS / 60) / 60)
        Dim minutes As Double = (hours Mod 1) * 60
        Dim seconds As Double = (minutes Mod 1) * 60
        Dim millisecond As Double = (totalSS Mod 1) 'Math.Round(totalSS Mod 1, 2, MidpointRounding.AwayFromZero) * 100
        Dim hrString As String = Math.Truncate(hours).ToString.PadLeft(2, "0")
        Dim minString As String = Math.Truncate(minutes).ToString.PadLeft(2, "0")
        Dim secString As String = Math.Truncate(seconds).ToString.PadLeft(2, "0")
        Dim milliString As String = millisecond.ToString("R")
        If milliString.Length > 2 Then
            milliString = milliString.Substring(2)
        End If
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
    ''' Get an array of bytes from the given image in 8888 BGRA form
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
    ''' Write the given byte array to the image in 8888 BGRA form
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

        If backColor Is Nothing Then
            'Set backcolor as most common color of the four corners
            Dim possibleColors As New List(Of Color)
            possibleColors.Add(imageBytes.GetPixel(startingRect.Left, startingRect.Top, stride, 4))
            possibleColors.Add(imageBytes.GetPixel(startingRect.Right - 1, startingRect.Top, stride, 4))
            possibleColors.Add(imageBytes.GetPixel(startingRect.Right - 1, startingRect.Bottom - 1, stride, 4))
            possibleColors.Add(imageBytes.GetPixel(startingRect.Left, startingRect.Bottom - 1, stride, 4))

            'Do three passes to find a common color, one that shows up at least 2 times
            For index As Integer = 0 To 2
                Dim uniqueColors As List(Of Color) = possibleColors.Distinct(Function(clr1, clr2) clr1.CompareTo(clr2))
                Dim occurance As New List(Of Integer)
                Dim multiPassIndex As Integer = index
                For uniqueColorIndex As Integer = 0 To uniqueColors.Count - 1
                    Dim uniqueLambaIndex As Integer = uniqueColorIndex
                    occurance.Add(possibleColors.FindAll(Function(obj As Color)
                                                             Return obj.Equivalent(uniqueColors(uniqueLambaIndex), (multiPassIndex * 2) + 1)
                                                         End Function).Count)
                Next
                backColor = uniqueColors(occurance.IndexOf(occurance.Max()))
                If occurance.Max() >= 2 Then
                    Exit For
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
                If (backColor.Value.A = 0 AndAlso srcPix.A = 0) OrElse (backColor.Value.Equivalent(srcPix, 5)) OrElse (backColor.Value.A = 0 AndAlso srcPix.A < alphaLimit) Then
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
                If (backColor.Value.A = 0 AndAlso srcPix.A = 0) OrElse (backColor.Value.Equivalent(srcPix, 5)) OrElse (backColor.Value.A = 0 AndAlso srcPix.A < alphaLimit) Then
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
                If (backColor.Value.A = 0 AndAlso srcPix.A = 0) OrElse (backColor.Value.Equivalent(srcPix, 5)) OrElse (backColor.Value.A = 0 AndAlso srcPix.A < alphaLimit) Then
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
                If (backColor.Value.A = 0 AndAlso srcPix.A = 0) OrElse (backColor.Value.Equivalent(srcPix, 5)) OrElse (backColor.Value.A = 0 AndAlso srcPix.A < alphaLimit) Then
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
        Dim left As Integer = startingRect.X
        Dim top As Integer = startingRect.Y
        Dim right As Integer = startingRect.X + startingRect.Width - 1
        Dim bottom As Integer = startingRect.Y + startingRect.Height - 1
        Dim centerY As Integer = startingRect.Center.Y
        Dim centerX As Integer = startingRect.Center.X

        'Left line check
        Dim hasExpanded As Integer = 1
        Dim currentBounds As Rectangle = New Rectangle(left, top, right - left, bottom - top)
        While hasExpanded > 0
            hasExpanded = 0
            Dim leftExp As Integer = ExpandBoundEdge(img1, currentBounds, 0)
            If leftExp <> left Then
                hasExpanded += 1
                left = leftExp
            End If
            Dim topExp As Integer = ExpandBoundEdge(img1, currentBounds, 1)
            If topExp <> top Then
                hasExpanded += 1
                top = topExp
            End If
            Dim rightExp As Integer = ExpandBoundEdge(img1, currentBounds, 2)
            If rightExp <> right Then
                hasExpanded += 1
                right = rightExp
            End If
            Dim bottomExp As Integer = ExpandBoundEdge(img1, currentBounds, 3)
            If bottomExp <> bottom Then
                hasExpanded += 1
                bottom = bottomExp
            End If

            currentBounds = New Rectangle(left, top, (right - left) + 1, (bottom - top) + 1)
        End While

        Dim potentialRect As Rectangle = New Rectangle(left, top, (right - left) + 1, (bottom - top) + 1)
        'As the algorithm continues until finding a solid border, it would create a rectangle including a 1px border, so we should remove it
        If potentialRect.Area > startingRect.Area Then
            Dim bestX As Integer = Math.Min(potentialRect.X + 1, startingRect.X)
            Dim bestY As Integer = Math.Min(potentialRect.Y + 1, startingRect.Y)
            Dim bestRight As Integer = Math.Max(potentialRect.Right - 2, startingRect.Right - 1)
            Dim bestBottom As Integer = Math.Max(potentialRect.Bottom - 2, startingRect.Bottom - 1)
            Return New Rectangle(bestX, bestY, (bestRight - bestX) + 1, (bestBottom - bestY) + 1)
        End If
        Return New Rectangle(left, top, (right - left) + 1, (bottom - top) + 1)
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

        For index As Integer = startIndex To endIndex Step (If(startIndex > endIndex, -1, 1))
            Dim xIndex As Integer = If(isHorizontal, index, startPerp)
            Dim yIndex As Integer = If(isHorizontal, startPerp, index)
            Dim pixIndex As Integer = (xIndex * 4) + yIndex * stride
            Dim startPixel As Color = imageBytes.GetPixel(xIndex, yIndex, stride, 4)
            Dim areEquivalent As Boolean = True
            'Expand perpendicular
            For perpIndex As Integer = startPerp To endPerp1 Step -1
                xIndex = If(isHorizontal, index, perpIndex)
                yIndex = If(isHorizontal, perpIndex, index)
                pixIndex = (xIndex * 4) + yIndex * stride
                Dim srcPix As Color = imageBytes.GetPixel(xIndex, yIndex, stride, 4)
                If Not srcPix.Equivalent(startPixel, 5) Then
                    areEquivalent = False
                    Exit For
                End If
            Next
            If Not areEquivalent Then
                Continue For
            End If
            'Expand perpendicular the other way
            For perpIndex As Integer = startPerp To endPerp2
                xIndex = If(isHorizontal, index, perpIndex)
                yIndex = If(isHorizontal, perpIndex, index)
                pixIndex = (xIndex * 4) + yIndex * stride
                Dim srcPix As Color = imageBytes.GetPixel(xIndex, yIndex, stride, 4)
                If Not srcPix.Equivalent(startPixel, 5) Then
                    areEquivalent = False
                    Exit For
                End If
            Next
            If areEquivalent Then
                Return index
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
    ''' Converts point to pointf with simple truncation
    ''' </summary>
    <Extension>
    Public Function ToPointF(pt As Point) As PointF
        Return New PointF(pt.X, pt.Y)
    End Function

    ''' <summary>
    ''' Converts pointf to point with simple truncation
    ''' </summary>
    <Extension>
    Public Function ToPoint(pt As PointF) As Point
        Return New Point(pt.X, pt.Y)
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
    ''' Sets the image of a picturebox, ensuring the previous image is disposed
    ''' </summary>
    <Extension>
    Public Sub SetImage(pictureBox As PictureBoxPlus, newImage As Image)
        If Not CacheFullBitmaps AndAlso pictureBox.Image IsNot Nothing Then
            pictureBox.Image.Dispose()
        End If
        If newImage IsNot Nothing Then
            If newImage.Size.FitScale(pictureBox.Size) > 1 Then
                pictureBox.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
            Else
                pictureBox.InterpolationMode = Drawing2D.InterpolationMode.Default
            End If
            pictureBox.Image = newImage
        Else
            pictureBox.Image = Nothing
        End If
    End Sub

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
End Module

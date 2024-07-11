Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Xml
Imports System.Xml.Serialization
Imports SimpleVideoEditor

<Serializable()>
Public Class ImageCache

    <Serializable()>
    Public Class CacheItem
        Public Sub New(temporary As Boolean)
            mblnTemporary = temporary
        End Sub

        ''' <summary>
        ''' Immediately converts stored image data into a 32bppArgb bitmap and returns the new instance
        ''' Remember to dispose afterwards
        ''' </summary>
        Public ReadOnly Property GetImage As Bitmap
            Get
                SyncLock Me
                    'Ensure 32bppArgb because some code depends on it like autocrop or just getting bytes of the image
                    'We want raw format to be memoryBmp, because otherwise things like lockbits may toss generic GDI+ errors
                    If (CacheFullBitmaps And Not mblnTemporary) AndAlso ImageStore IsNot Nothing Then
                        Return ImageStore
                    End If
                    If ImageData IsNot Nothing Then
                        Using tempStream As New MemoryStream(ImageData)
                            Dim incomingBitmap As Bitmap = New Bitmap(tempStream)
                            If incomingBitmap.PixelFormat <> Imaging.PixelFormat.Format32bppArgb OrElse Not incomingBitmap.RawFormat.Equals(Imaging.ImageFormat.MemoryBmp) Then
                                Dim newBitmap As New Bitmap(incomingBitmap.Width, incomingBitmap.Height, Imaging.PixelFormat.Format32bppArgb)
                                Using g As Graphics = Graphics.FromImage(newBitmap)
                                    g.DrawImage(incomingBitmap, New Point(0, 0))
                                End Using
                                incomingBitmap.Dispose()
                                incomingBitmap = newBitmap
                            End If
                            ImageStore = incomingBitmap
                            If (CacheFullBitmaps And Not mblnTemporary) Then
                                ImageData = {0} 'Release memory so the next garbage collect can clean up
                            End If
                            Return ImageStore
                        End Using
                    Else
                        Return Nothing
                    End If
                End SyncLock
            End Get
        End Property

        Private mblnTemporary As Boolean = False 'Overrides full bitmap caching to ensure the contents of the cache can be cleared without worry of external usage

        Private mobjImgSize As Size?

        Public ReadOnly Property Width
            Get
                Return Size.Width
            End Get
        End Property

        Public ReadOnly Property Height
            Get
                Return Size.Width
            End Get
        End Property

        Public ReadOnly Property Size As Size
            Get
                If mobjImgSize Is Nothing Then
                    If CacheFullBitmaps Then
                        mobjImgSize = Me.GetImage().Size
                    Else
                        Using tempMap As Image = Me.GetImage()
                            mobjImgSize = tempMap.Size
                        End Using
                    End If
                End If
                Return mobjImgSize
            End Get
        End Property


        ''' <summary>Storage for the stream data converted into a bitmap</summary>
        Private ImageStore As Bitmap

        ''' <summary>Storage for the raw stream data provided by ffmpeg, hopefully compressed reasonably well</summary>
        <XmlIgnore>
        Public ImageData As Byte()
        ''' <summary>Time that the image was first queued up for retrieval</summary>
        Public QueueTime As DateTime?
        ''' <summary>Presentation time of the image</summary>
        Public PTSTime As Double?

        ''' <summary>
        ''' Gets bytes from bitmap. Created for serialization
        ''' </summary>
        'Public Property BitmapBytes() As Byte()
        '    Get
        '        If Me.Image Is Nothing Then
        '            Return Nothing
        '        Else
        '            Using memStream As New MemoryStream
        '                'The reason we have to do this is because the memory stream the bitmap
        '                'was created with has already been long destroyed
        '                Using tempMap As New Bitmap(Me.Image)
        '                    tempMap.Save(memStream, Imaging.ImageFormat.Bmp)
        '                    Return memStream.ToArray()
        '                End Using
        '            End Using
        '        End If
        '    End Get
        '    Set(value As Byte())
        '        If value Is Nothing Then
        '            Me.Image = Nothing
        '        Else
        '            Using memStream As New MemoryStream
        '                memStream.Write(value, 0, value.Count)
        '                Me.Image = Bitmap.FromStream(memStream)
        '                Dim imageData As BitmapData = Image.LockBits(New Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb)

        '                Dim byteTotal As Integer = imageData.Stride * imageData.Height
        '                Dim scanLineTotal As Integer = imageData.Width * 3
        '                Dim pixelBytesTotal As Integer = scanLineTotal * imageData.Height
        '                If mbytPixels Is Nothing OrElse Not mbytPixels.Length = pixelBytesTotal Then
        '                    ReDim mbytPixels(pixelBytesTotal - 1)
        '                End If
        '                For scanIndex As Integer = 0 To imageData.Height - 1
        '                    Marshal.Copy(imageData.Scan0 + scanIndex * imageData.Stride, mbytPixels, scanIndex * scanLineTotal, scanLineTotal)
        '                Next
        '                Image.UnlockBits(imageData)
        '            End Using
        '        End If
        '    End Set
        'End Property

        <XmlIgnore>
        Private mbytPixels As Byte()
        <XmlIgnore>
        Private mbytAvgColor As Byte()
        <XmlIgnore>
        Private mdblStdDev As Double()

        ''' <summary>
        ''' Returns byte array of RGB color averages from mbytPixels
        ''' </summary>
        Public Function AverageColor() As Byte()
            If mbytAvgColor Is Nothing Then
                mbytAvgColor = New Byte(2) {}
                Dim rSum As Integer = 0
                Dim gSum As Integer = 0
                Dim bSum As Integer = 0
                For index As Integer = 0 To mbytPixels.Count - 1 Step 3
                    rSum += mbytPixels(index)
                    gSum += mbytPixels(index + 1)
                    bSum += mbytPixels(index + 2)
                Next
                mbytAvgColor(0) = rSum / mbytPixels.Count
                mbytAvgColor(1) = gSum / mbytPixels.Count
                mbytAvgColor(2) = bSum / mbytPixels.Count
            End If
            Return mbytAvgColor
        End Function

        ''' <summary>
        ''' Calculates standard deviation of each color component in the image
        ''' </summary>
        ''' <returns></returns>
        Public Function StdDev() As Double()
            If mdblStdDev Is Nothing Then
                mdblStdDev = New Double(2) {}
                Dim rSum As Integer = 0
                Dim gSum As Integer = 0
                Dim bSum As Integer = 0
                For index As Integer = 0 To mbytPixels.Count - 1 Step 3
                    rSum += mbytPixels(index)
                    gSum += mbytPixels(index + 1)
                    bSum += mbytPixels(index + 2)
                Next
                Dim rAvg As Double = rSum / mbytPixels.Count
                Dim gAvg As Double = gSum / mbytPixels.Count
                Dim bAvg As Double = bSum / mbytPixels.Count

                'We already calculated it so we might as well save the computation for later
                If mbytAvgColor Is Nothing Then
                    mbytAvgColor = New Byte(2) {}
                    mbytAvgColor(0) = rAvg
                    mbytAvgColor(1) = gAvg
                    mbytAvgColor(2) = bAvg
                End If

                Dim rSqr As Double = 0
                Dim gSqr As Double = 0
                Dim bSqr As Double = 0
                For index As Integer = 0 To mbytPixels.Count - 1 Step 3
                    rSqr += Math.Pow(mbytPixels(index) - rAvg, 2)
                    gSqr += Math.Pow(mbytPixels(index + 1) - gAvg, 2)
                    bSqr += Math.Pow(mbytPixels(index + 2) - bAvg, 2)
                Next
                mdblStdDev(0) = Math.Sqrt(rSqr / mbytPixels.Count)
                mdblStdDev(1) = Math.Sqrt(gSqr / mbytPixels.Count)
                mdblStdDev(2) = Math.Sqrt(bSqr / mbytPixels.Count)
            End If
            Return mdblStdDev
        End Function

        Public Overrides Function ToString() As String
            Dim queueString As String = If(QueueTime?.ToString, "null")
            Dim bitmapString As String = If(GetImage IsNot Nothing, $"{Me.Width}x{Me.Height}", "null")
            Return $"{Me.Status.ToString} | Queued: {queueString} | {bitmapString} | PTS: {Me.PTSTime}"
        End Function

        ''' <summary>
        ''' Checks that two frames are essentially the same, within margin 0-100% similar
        ''' </summary>
        Public Function EqualsWithin(value As CacheItem, margin As Double)
            Dim difStdDev As Double = CompareArrays(Me.StdDev, value.StdDev)
            If difStdDev > 1 Then
                Return False
            End If
            Dim difFullyCompressed As Double = CompareArraysAvg(Me.AverageColor, value.AverageColor)
            Return margin >= difFullyCompressed

            'Short circuit so we don't waste a ton of resources trying to calculate
            If margin < difFullyCompressed Then
                Return False
            End If
            Dim difAvg As Double = CompareArraysAvg(Me.mbytPixels, value.mbytPixels)

            Return margin >= difAvg
        End Function

        Public Function Status() As CacheStatus
            If Me.ImageData Is Nothing AndAlso Me.QueueTime Is Nothing Then
                Return CacheStatus.None
            End If
            If Me.ImageData IsNot Nothing Then
                Return CacheStatus.Cached
            ElseIf Me.QueueTime IsNot Nothing Then
                Return CacheStatus.Queued
            End If
            Return CacheStatus.None
        End Function

        Public Sub ClearImageData()
            If Me.ImageStore IsNot Nothing Then
                Me.ImageStore.Dispose()
                Me.ImageStore = Nothing
            End If
            Me.ImageData = Nothing
        End Sub
    End Class

    Public Enum CacheStatus
        None = 0
        Queued = 1
        Cached = 2
    End Enum

    Private mobjCollection() As CacheItem
    Private mblnTemporary As Boolean = False

    Public Sub New()
    End Sub

    Public Sub New(totalFrames As Integer, Optional temporary As Boolean = False)
        Me.mblnTemporary = temporary
        mobjCollection = New CacheItem(totalFrames - 1) {}
        For index As Integer = 0 To totalFrames - 1
            mobjCollection(index) = New CacheItem(temporary)
        Next
    End Sub

    Public ReadOnly Property Temporary As Boolean
        Get
            Return mblnTemporary
        End Get
    End Property

    Default ReadOnly Property Item(index As Integer) As CacheItem
        Get
            Return mobjCollection(index)
        End Get
    End Property

    Public Property Items() As CacheItem()
        Get
            Return mobjCollection
        End Get
        Set(value As CacheItem())
            mobjCollection = value
        End Set
    End Property
    ''' <summary>
    ''' Checks if a given frame is already cached in memory, has been queued, or has not been/is out of bounds(none)
    ''' </summary>
    ''' <param name="intFrame"></param>
    ''' <returns></returns>
    Public Function ImageCacheStatus(intFrame As Integer) As CacheStatus
        If intFrame < 0 OrElse intFrame >= Me.mobjCollection.Count Then
            Return CacheStatus.None
        Else
            Return Me.mobjCollection(intFrame).Status
        End If
    End Function

    ''' <summary>
    ''' Clears the image cache and ensures it is of the proper size
    ''' </summary>
    Public Sub ClearImageCache()
        SyncLock Me
            For index As Integer = 0 To mobjCollection.Count - 1
                If Not Temporary Then
                End If
                mobjCollection(index).ClearImageData()
                mobjCollection(index).QueueTime = Nothing
            Next
        End SyncLock
    End Sub

    ''' <summary>
    ''' Attempts to set the range of frames queue times to now, returns the number that had their times set (were not already set)
    ''' </summary>
    ''' <param name="startFrame"></param>
    ''' <param name="endFrame"></param>
    ''' <returns></returns>
    Public Function TryQueue(startFrame As Integer, endFrame As Integer) As Integer
        Dim itemsQueued As Integer = 0
        For index As Integer = startFrame To endFrame
            If mobjCollection(index).Status = CacheStatus.None Then
                mobjCollection(index).QueueTime = Now
                itemsQueued += 1
            End If
        Next
        Return itemsQueued
    End Function

    ''' <summary>
    ''' Checks the given range, compressing it if frames are already detected as queued or cached in the range. Will return nothing if all frames are already ok
    ''' </summary>
    Public Function TrimRange(startFrame As Integer, endFrame As Integer) As List(Of Integer)
        Dim resultRange As New List(Of Integer) From {startFrame, endFrame}
        Dim frameNeeded As Boolean = False
        'Find first non-cached frame
        For index As Integer = startFrame To endFrame
            If mobjCollection(index).Status = CacheStatus.None Then
                resultRange(0) = index
                frameNeeded = True
                Exit For
            End If
        Next
        For index As Integer = endFrame To startFrame Step -1
            If mobjCollection(index).Status = CacheStatus.None Then
                resultRange(1) = index
                frameNeeded = True
                Exit For
            End If
        Next
        If frameNeeded Then
            Return resultRange
        Else
            Return Nothing
        End If
    End Function

#Region "Serialization"
    ''' <summary>
    ''' Serialize to xml file
    ''' </summary>
    Public Sub SaveToFile(ByVal filePath As String)
        Using fileStream = New FileStream(filePath, FileMode.Create, FileAccess.Write)
            Using streamWriter = New StreamWriter(fileStream)
                Dim xmlSerializer As New XmlSerializer(GetType(ImageCache))
                xmlSerializer.Serialize(streamWriter, Me)
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' Deserialize from a file, returns nothing if the file doesn't exist
    ''' </summary>
    Public Shared Function ReadFromFile(ByVal filePath As String) As ImageCache
        If File.Exists(filePath) Then
            Dim newCache As ImageCache
            Using fileStream = New FileStream(filePath, FileMode.Open, FileAccess.Read)
                Using streamReader As XmlTextReader = New XmlTextReader(fileStream)
                    Dim xmlSerializer As New XmlSerializer(GetType(ImageCache))
                    newCache = xmlSerializer.Deserialize(streamReader)
                End Using
            End Using
            Return newCache
        Else
            Return Nothing
        End If
    End Function
#End Region
End Class
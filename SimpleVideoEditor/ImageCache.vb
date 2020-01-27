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
        <XmlIgnore>
        Public Image As Bitmap
        Public QueueTime As DateTime?

        ''' <summary>
        ''' Gets bytes from bitmap. Created for serialization
        ''' </summary>
        Public Property BitmapBytes() As Byte()
            Get
                If Me.Image Is Nothing Then
                    Return Nothing
                Else
                    Using memStream As New MemoryStream
                        'The reason we have to do this is because the memory stream the bitmap
                        'was created with has already been long destroyed
                        Using tempMap As New Bitmap(Me.Image)
                            tempMap.Save(memStream, Imaging.ImageFormat.Bmp)
                            Return memStream.ToArray()
                        End Using
                    End Using
                End If
            End Get
            Set(value As Byte())
                If value Is Nothing Then
                    Me.Image = Nothing
                Else
                    Using memStream As New MemoryStream
                        memStream.Write(value, 0, value.Count)
                        Me.Image = Bitmap.FromStream(memStream)
                        Dim imageData As BitmapData = Image.LockBits(New Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb)

                        Dim byteTotal As Integer = imageData.Stride * imageData.Height
                        Dim scanLineTotal As Integer = imageData.Width * 3
                        Dim pixelBytesTotal As Integer = scanLineTotal * imageData.Height
                        If mbytPixels Is Nothing OrElse Not mbytPixels.Length = pixelBytesTotal Then
                            ReDim mbytPixels(pixelBytesTotal - 1)
                        End If
                        For scanIndex As Integer = 0 To imageData.Height - 1
                            Marshal.Copy(imageData.Scan0 + scanIndex * imageData.Stride, mbytPixels, scanIndex * scanLineTotal, scanLineTotal)
                        Next
                        Image.UnlockBits(imageData)
                    End Using
                End If
            End Set
        End Property

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
            Dim bitmapString As String = If(Image IsNot Nothing, $"{Image.Width}x{Image.Height}", "null")
            Return $"Queued: {queueString} | {bitmapString}"
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
            If Me.Image Is Nothing AndAlso Me.QueueTime Is Nothing Then
                Return CacheStatus.None
            End If
            If Me.Image IsNot Nothing Then
                Return CacheStatus.Cached
            ElseIf Me.QueueTime IsNot Nothing Then
                Return CacheStatus.Queued
            End If
            Return CacheStatus.None
        End Function
    End Class

    Public Enum CacheStatus
        None = 0
        Queued = 1
        Cached = 2
    End Enum

    Private mobjCollection() As CacheItem
    Public Sub New()
    End Sub

    Public Sub New(totalFrames As Integer)
        mobjCollection = New CacheItem(totalFrames - 1) {}
        For index As Integer = 0 To totalFrames - 1
            mobjCollection(index) = New CacheItem()
        Next
    End Sub

    Default ReadOnly Property Item(index As Integer)
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
        For index As Integer = 0 To mobjCollection.Count - 1
            If mobjCollection(index).Image IsNot Nothing Then
                mobjCollection(index).Image.Dispose()
                mobjCollection(index).Image = Nothing
            End If
            mobjCollection(index).QueueTime = Nothing
        Next
    End Sub

    Public Sub SetQueue(startFrame As Integer, endFrame As Integer)
        For index As Integer = startFrame To endFrame
            mobjCollection(index).QueueTime = Now
        Next
    End Sub

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
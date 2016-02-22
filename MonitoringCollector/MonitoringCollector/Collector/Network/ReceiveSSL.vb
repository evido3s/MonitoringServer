﻿Imports MonitoringCollector.ServerParameters
Imports MonitoringCollector.MonitoringDatabase
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports System.Text
Imports System.IO
Imports System.Xml
Imports System.Security.Cryptography.X509Certificates
Imports System.Net.Security
Imports System.Security.Authentication

Public Class ReceiveSSL

    Public ListenPort As Integer = TCPListenPort
    Public ListenAddress As IPAddress = IPAddress.Any
    Public tcpListener As TcpListener = New TcpListener(ListenAddress, ListenPort)
    Public Shared serverCertificate As New X509Certificate2(ServerPath & "config\certificate.pfx", CertificatePassword)

    Public Sub StartListener()
        tcpListener.Start()
        While True
            DoBeginAcceptTcpClient(tcpListener)
        End While
    End Sub

    Public tcpClientConnected As New ManualResetEvent(False)

    Public Sub DoBeginAcceptTcpClient(tcpListener As TcpListener)
        tcpClientConnected.Reset()
        ' Accept the connection. 
        tcpListener.BeginAcceptTcpClient(New AsyncCallback(AddressOf DoAcceptTcpClientCallback), tcpListener)
        tcpClientConnected.WaitOne()
    End Sub

    Public Sub DoAcceptTcpClientCallback(ar As IAsyncResult)
        ' Get the listener that handles the client request.

        Try
            Dim tcpListener As TcpListener = CType(ar.AsyncState, TcpListener)
            Dim tcpClient As TcpClient = tcpListener.EndAcceptTcpClient(ar)

            Dim sslStream As New SslStream(tcpClient.GetStream)
            sslStream.AuthenticateAsServer(serverCertificate, False, SslProtocols.Tls12, False)
            sslStream.ReadTimeout = 15000
            sslStream.WriteTimeout = 15000

            Dim Message As String = Nothing

            Dim Reader As New StreamReader(sslStream)
            While Reader.Peek > -1
                Message = Message + Convert.ToChar(Reader.Read)
            End While

            Dim ResponseString As String = "Data received by server"
            Dim ResponseBytes As Byte() = Encoding.UTF8.GetBytes(ResponseString)

            sslStream.Write(ResponseBytes, 0, ResponseBytes.Length)
            sslStream.Close()
            Dim Compression As New Compression
            TranslateXML(Compression.DecompressData(Message))

        Catch ex As Exception

        Finally
            tcpClientConnected.Set()
        End Try


    End Sub




    Public Sub TranslateXML(ByVal xmlMessage As String)

        Dim db As New DBModel
        Dim AgentName As String = Nothing
        Dim AgentDomain As String = Nothing
        Dim AgentIP As String = Nothing
        Dim AgentOSName As String = Nothing
        Dim AgentOSBuild As String = Nothing
        Dim AgentOSArchitecture As String = Nothing
        Dim AgentProcessors As String = Nothing
        Dim AgentMemory As String = Nothing
        Dim AgentDate As String = Nothing

        Try
            Dim doc As New XmlDocument
            doc.LoadXml(xmlMessage)

            Dim AgentSystemNode As XmlNodeList = doc.DocumentElement.SelectNodes("/Agent/AgentSystem")
            Dim AgentDataNode As XmlNodeList = doc.DocumentElement.SelectNodes("/Agent/AgentData")

            For Each node As XmlNode In AgentSystemNode
                AgentName = node.SelectSingleNode("AgentName").InnerText
                AgentDomain = node.SelectSingleNode("AgentDomain").InnerText
                AgentIP = node.SelectSingleNode("AgentIP").InnerText
                AgentOSName = node.SelectSingleNode("AgentOSName").InnerText
                AgentOSBuild = node.SelectSingleNode("AgentOSBuild").InnerText
                AgentOSArchitecture = node.SelectSingleNode("AgentOSArchitecture").InnerText
                AgentProcessors = node.SelectSingleNode("AgentProcessor").InnerText
                AgentMemory = node.SelectSingleNode("AgentMemory").InnerText
                AgentDate = node.SelectSingleNode("AgentDate").InnerText

                Dim Q = (From T In db.AgentSystem
                         Where T.AgentName = AgentName
                         Select T).FirstOrDefault

                If Q IsNot Nothing Then
                    Q.AgentDomain = AgentDomain
                    Q.AgentIP = AgentIP
                    Q.AgentOSName = AgentOSName
                    Q.AgentOSBuild = AgentOSBuild
                    Q.AgentOSArchitecture = AgentOSArchitecture
                    Q.AgentProcessors = AgentProcessors
                    Q.AgentMemory = AgentMemory
                    Q.AgentDate = AgentDate
                    db.SaveChanges()
                Else
                    db.AgentSystem.Add(New AgentSystem With {.AgentName = AgentName, .AgentDomain = AgentDomain, .AgentIP = AgentIP, .AgentOSName = AgentOSName, .AgentOSBuild = AgentOSBuild, .AgentOSArchitecture = AgentOSArchitecture, .AgentProcessors = .AgentProcessors, .AgentMemory = .AgentMemory, .AgentDate = AgentDate})
                End If

            Next

            Dim AgentClass As String = Nothing
            Dim AgentProperty As String = Nothing
            Dim AgentValue As String = Nothing

            For Each node As XmlNode In AgentDataNode
                AgentClass = node.SelectSingleNode("AgentClass").InnerText
                AgentProperty = node.SelectSingleNode("AgentProperty").InnerText
                AgentValue = node.SelectSingleNode("AgentValue").InnerText

                Select Case True
                    Case AgentClass.Contains("Processor")
                        db.AgentProcessor.Add(New AgentProcessor With {.AgentName = AgentName, .AgentClass = AgentClass, .AgentProperty = AgentProperty, .AgentValue = AgentValue, .AgentCollectDate = AgentDate})
                        db.SaveChanges()
                    Case AgentClass.Contains("Memory")
                        db.AgentMemory.Add(New AgentMemory With {.AgentName = AgentName, .AgentClass = AgentClass, .AgentProperty = AgentProperty, .AgentValue = AgentValue, .AgentCollectDate = AgentDate})
                        db.SaveChanges()
                    Case AgentClass.Contains("PageFile")
                        db.AgentPageFile.Add(New AgentPageFile With {.AgentName = AgentName, .AgentClass = AgentClass, .AgentProperty = AgentProperty, .AgentValue = AgentValue, .AgentCollectDate = AgentDate})
                        db.SaveChanges()
                    Case AgentClass.Contains("Local Disk")
                        db.AgentLocalDisk.Add(New AgentLocalDisk With {.AgentName = AgentName, .AgentClass = AgentClass, .AgentProperty = AgentProperty, .AgentValue = AgentValue, .AgentCollectDate = AgentDate})
                        db.SaveChanges()
                    Case AgentClass.Contains("Services")
                        db.AgentService.Add(New AgentService With {.AgentName = AgentName, .AgentClass = AgentClass, .AgentProperty = AgentProperty, .AgentValue = AgentValue, .AgentCollectDate = AgentDate})
                        db.SaveChanges()
                End Select

            Next
        Catch
        End Try




    End Sub



End Class

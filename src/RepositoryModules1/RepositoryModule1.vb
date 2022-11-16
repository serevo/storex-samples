﻿Imports System.IO
Imports System.Text
Imports System.Threading
Imports Storex

<RepositoryModuleExport("E0B3F83A-B417-43DB-8CCF-9916A2EB63C6", "簡易ファイルシステム", Description:="モードでシンボル内容等を設定します")>
Public Class RepositoryModule1
    Implements IRepositoryModule

    Friend ReadOnly TextEncoding As Encoding = Encoding.GetEncoding("shift_jis")

    Dim CurrentMode As IMode
    Dim CurrentUser As IUser

    Public ReadOnly Property IsConfiguable As Boolean = True Implements IRepositoryModule.IsConfiguable

    Public ReadOnly Property IsModeConfiguable As Boolean = True Implements IRepositoryModule.CanConfigureMode

    Public Function ConfigureAsync() As Task Implements IModule.ConfigureAsync

        ConfigForm1.Configure()

        Return Task.CompletedTask
    End Function

    Public Function ConfigureModeAsync(Mode As IMode) As Task Implements IRepositoryModule.ConfigureModeAsync

        ModeConfigForm1.Configure(Mode)

        Return Task.CompletedTask
    End Function

    Public Function PrepareAsync(Mode As IMode, UserInfo As IUser) As Task Implements IRepositoryModule.PrepareAsync

        If Not File.Exists(My.Settings.FilePath) Then

            Throw New RepositoryModuleException("概要データ ファイルが正しく設定されていません。")
        End If

        If Not Directory.Exists(My.Settings.FolderPath) Then

            Throw New RepositoryModuleException("詳細データ フォルダが正しく設定されていません。")
        End If

        If Not LabelDefinition1.TryExtract(Mode, LabelDefinition1.KeyForPrimary) Then

            Throw New RepositoryModuleException("モードが構成されていません。")
        End If

        CurrentMode = Mode
        CurrentUser = UserInfo

        Return Task.CompletedTask
    End Function

    Public Function FindPrimaryLabel(Sources() As ILabelSource) As ILabel Implements IRepositoryModule.FindPrimaryLabel

        Dim MatchSymbols = Sources _
            .OfType(Of Symbol) _
            .Select(
                Function(x)

                    Dim LabelDefinition As LabelDefinition1 = Nothing
                    Dim Label As BasicLabel = Nothing
                    Dim IsMatch = LabelDefinition1.TryExtract(CurrentMode, LabelDefinition1.KeyForPrimary, LabelDefinition) AndAlso LabelDefinition.TryGeneraLabel(x, Label)

                    Return New With {IsMatch, Label}
                End Function
                ) _
            .Where(Function(x) x.IsMatch) _
            .Select(Function(x) x.Label) _
            .ToArray()

        Return If(MatchSymbols.Length = 1, MatchSymbols(0), Nothing)
    End Function

    Public Function FindSecondaryLabels(Primary As ILabel, Sources() As ILabelSource) As ILabel() Implements IRepositoryModule.FindSecondaryLabels

        If CurrentMode.HasSecondaryLabel Then

            Dim MatchSymbols = Sources _
                .OfType(Of C3Label) _
                .Where(Function(x) x.PartNumber = Primary.ItemNumber) _
                .ToArray()

            Return MatchSymbols
        End If

        Return Array.Empty(Of ILabel)
    End Function

    Public Function RegisterAsync(Primary As ILabel, Secondary As ILabel, CaptureDatas() As CaptureData, Tags() As String) As Task Implements IRepositoryModule.RegisterAsync

        Return RegisterAsync(Primary, Secondary, CaptureDatas, Tags, CancellationToken.None)
    End Function

    Public Async Function RegisterAsync(Primary As ILabel, Secondary As ILabel, CaptureDatas() As CaptureData, Tags() As String, cancellationToken As CancellationToken) As Task Implements IRepositoryModule.RegisterAsync

        Dim Timestamp As Date = Now

        Dim MyFile = New FileInfo(My.Settings.FilePath)

        Using SW = New StreamWriter(MyFile.FullName, True, TextEncoding)

            If MyFile.Length = 0 Then

                Await SW.WriteLineAsync(String.Join(vbTab, New String() {
                    "PrimaryPartNumber",
                    "PrimarySerialNumber",
                    "SecondaryPartNumber",
                    "SecondarySerialNumber",
                    "ModeId",
                    "UserName",
                    "Timestamp"
                    }))
            End If

            Await SW.WriteLineAsync(String.Join(vbTab, New String() {
                Primary.ItemNumber,
                Primary.SerialNumber,
                Secondary?.ItemNumber,
                Secondary?.SerialNumber,
                CurrentMode.Id.ToString(),
                CurrentUser?.DisplayName,
                $"{Timestamp:yyyy/MM/dd HH:mm:ss.fff}"
                }))
        End Using

        Dim MyFolder = New DirectoryInfo(My.Settings.FolderPath)

        MyFolder = MyFolder.CreateSubdirectory($"{Timestamp:yyyy-MM-dd HH-mm-ss.fff}")

        For i = 1 To CaptureDatas.Count

            Dim CaptureData As CaptureData = CaptureDatas(i - 1)

            Dim CaptureDataFolder = MyFolder.CreateSubdirectory($"catpure#{i}")

            File.WriteAllBytes($"{CaptureDataFolder.FullName}\original.jpg", CaptureData.OriginalImage)

            If CaptureData.AdornedImage IsNot Nothing Then

                File.WriteAllBytes($"{CaptureDataFolder.FullName}\adorned.jpeg", CaptureData.AdornedImage)
            End If

            If CaptureData.LabelSources.Count > 0 Then

                Dim SymbolValues() As String = CaptureData.LabelSources.SelectMany(Function(x) x.Symbols.Select(Function(xx) xx.Value)).ToArray()

                File.WriteAllLines($"{CaptureDataFolder.FullName}\symbols.txt", SymbolValues)
            End If
        Next

        If Tags.Length > 0 Then

            File.WriteAllLines($"{MyFolder.FullName}\tags.txt", Tags)
        End If
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
Imports System.Threading
Imports Storex

<AuthenticationModuleExport("96DF50EE-83EB-49B5-9191-266307F0792C", "簡易認証 (氏名選択) [VB]", Description:="CSVファイル (ID と 氏名) を使用します")>
Public Class UseCsvFileAuthenticationModule
    Implements IAuthenticationModule

    Public ReadOnly Property IsConfiguable As Boolean Implements IAuthenticationModule.IsConfiguable
        Get
            IsConfiguable = True
        End Get
    End Property

    Public Function ConfigureAsync() As Task Implements IAuthenticationModule.ConfigureAsync

        AuthenticationModuleHelper.PickUsersFileAndSaveToSetting()

        ConfigureAsync = Task.CompletedTask

    End Function

    Public Async Function AuthenticateAsync() As Task(Of IUser) Implements IAuthenticationModule.AuthenticateAsync

        Return Await AuthenticateAsync(cancellationToken:=CancellationToken.None)
    End Function

    Public Function AuthenticateAsync(cancellationToken As CancellationToken) As Task(Of IUser) Implements IAuthenticationModule.AuthenticateAsync

        Dim User As IUser = AuthenticationForm2.Authenticate

        Return Task.FromResult(User)

    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

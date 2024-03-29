﻿Imports System.Threading
Imports Storex

<AuthenticationModuleExport("FCB29577-7E5B-4B0C-A514-B8E636AAF13D", "簡易認証 (ID入力)", Description:="CSVファイル (ID と 氏名) を使用します")>
Public Class AuthenticationModule1
    Implements IAuthenticationModule

    Public ReadOnly Property IsConfiguable As Boolean Implements IAuthenticationModule.IsConfiguable
        Get
            IsConfiguable = True
        End Get
    End Property

    Public Function ConfigureAsync(cancellationToken As CancellationToken) As Task Implements IAuthenticationModule.ConfigureAsync

        AuthenticationModuleHelper.PickUsersFileAndSaveToSetting()

        Return Task.CompletedTask
    End Function

    Public Function AuthenticateAsync(cancellationToken As CancellationToken) As Task(Of IUser) Implements IAuthenticationModule.AuthenticateAsync

        Dim User As IUser = AuthenticationForm1.Authenticate

        Return Task.FromResult(User)
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

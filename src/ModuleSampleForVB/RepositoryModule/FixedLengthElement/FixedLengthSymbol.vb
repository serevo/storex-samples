﻿Imports Storex

Public Class FixedLengthSymbol
    Inherits Symbol

    Protected Overrides ReadOnly Property PartNumber As String

    Protected Overrides ReadOnly Property SerialNumber As String

    Public Shared ReadOnly ValidSymbolTypes() As SymbolType =
    {
       SymbolType.QrCode
    }

    Private Sub New(partNum As String, serialNum As String, type As SymbolType, value As String)
        MyBase.New(type, value)

        If String.IsNullOrEmpty(partNum) Then
            Throw New ArgumentException($"'{NameOf(partNum)}' を NULL または空にすることはできません。", NameOf(partNum))
        End If

        If String.IsNullOrEmpty(serialNum) Then
            Throw New ArgumentException($"'{NameOf(serialNum)}' を NULL または空にすることはできません。", NameOf(serialNum))
        End If

        PartNumber = partNum
        SerialNumber = serialNum
    End Sub

    Public Shared Function TryParse(symbol As Symbol, setting As FixedLengthRepositoryModeSetting, ByRef fixedLengthSymbol As FixedLengthSymbol) As Boolean



    End Function
End Class
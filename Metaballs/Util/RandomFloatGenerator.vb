Imports Random
Imports Math

' Uses singleton design pattern
Public Class RandomFloatGenerator
    Private Shared inst As New RandomFloatGenerator()
    Private Shared random As Random

    Public Shared Function instance() As RandomFloatGenerator
        Return inst
    End Function

    Public Function NextFloat() As Single
        Dim mantissa As Single = (random.NextDouble() * 1000.0)

        Return mantissa
    End Function

    Private Sub New()
        random = New Random()
    End Sub
End Class

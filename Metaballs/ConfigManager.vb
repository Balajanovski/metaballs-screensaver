Imports Microsoft.Win32

Public Class ConfigManager
    Public ReadOnly Property DropletNumber As Integer
    Public ReadOnly Property Speed As Double
    Public ReadOnly Property WaveHeight As Double
    Public ReadOnly Property ResolutionRatio As Double

    Private Shared inst As New ConfigManager()

    Public Shared ReadOnly Property Instance As ConfigManager
        Get
            Return inst
        End Get
    End Property

    Public Function ValueExists(key As RegistryKey, value As String) As Boolean
        Try
            Return Not IsNothing(key.GetValue(value))
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Sub New()
        ' Get the values stored in the Registry
        Dim key As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Metaballs_ScreenSaver")
        If key Is Nothing Or
            Not ValueExists(key, "dropletNumber") Or
            Not ValueExists(key, "speed") Or
            Not ValueExists(key, "waveheight") Or
            Not ValueExists(key, "resolutionRatio") Then

            DropletNumber = 20
            WaveHeight = 2.1
            Speed = 1.0
            ResolutionRatio = 1.0
        Else
            DropletNumber = Integer.Parse(key.GetValue("dropletNumber"))
            Speed = Math.Pow(2, Double.Parse(key.GetValue("speed")))
            WaveHeight = Double.Parse(key.GetValue("waveheight"))
            ResolutionRatio = Double.Parse(key.GetValue("resolutionRatio"))
        End If
    End Sub
End Class

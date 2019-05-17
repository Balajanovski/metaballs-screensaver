Imports Microsoft.Win32

Public Class ConfigManager
    Public ReadOnly Property DropletNumber As Integer
    Public ReadOnly Property Speed As Double
    Public ReadOnly Property WaveHeight As Double

    Private Shared inst As New ConfigManager()

    Public Shared ReadOnly Property Instance As ConfigManager
        Get
            Return inst
        End Get
    End Property

    Public Sub New()
        ' Get the values stored in the Registry
        Dim key As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Metaballs_ScreenSaver")
        If key Is Nothing Then
            DropletNumber = 20
            WaveHeight = 2.1
            Speed = 1.0
        Else
            DropletNumber = Integer.Parse(key.GetValue("dropletNumber"))
            Speed = Math.Pow(2, Double.Parse(key.GetValue("speed")))
            WaveHeight = Double.Parse(key.GetValue("waveheight"))
        End If
    End Sub
End Class

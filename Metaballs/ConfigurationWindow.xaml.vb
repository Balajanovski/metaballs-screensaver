﻿Imports Microsoft.Win32
Imports System.Windows.Forms
Imports System.Globalization

Public Class ConfigurationWindow

    Private savedSettings As Boolean = False

    Private initialising As Boolean = True

    Private languageChanged As Boolean = False

    Public Sub New(Optional selectedLanguage As Integer = 0)
        ' This call is required by the designer.
        InitializeComponent()

        savedSettings = False

        languageBox.SelectedIndex = selectedLanguage
        UpdateText()

        initialising = False
    End Sub

    Private Sub SaveSettings()
        savedSettings = True

        ' Create or get existing Registry subkey
        Dim key As RegistryKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Metaballs_ScreenSaver")

        key.SetValue("waveheight", waveheightSlider.Value)
        key.SetValue("speed", speedSlider.Value)
        key.SetValue("dropletNumber", dropletNumberSlider.Value)
        key.SetValue("resolutionRatio", CDbl(1.0 / resolutionSlider.Value))

        MessageBox.Show("Screensaver Settings Saved Successfully!", "Settings Saved!", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1)
    End Sub

    Private Sub Window_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs)
        If Not savedSettings And Not languageChanged Then
            Dim result As DialogResult = MessageBox.Show(My.Resources.Locale.ExitWithoutSavingMsg,
                                                      My.Resources.Locale.ExitWithoutSavingTitle,
                                                      MessageBoxButtons.YesNo,
                                                      MessageBoxIcon.Question,
                                                      MessageBoxDefaultButton.Button1)

            If result = Windows.Forms.DialogResult.Yes Then
                e.Cancel = False
            Else
                e.Cancel = True
            End If
        End If
    End Sub

    Private Sub SaveButton_Click(sender As Object, e As Windows.RoutedEventArgs) Handles saveButton.Click
        SaveSettings()
    End Sub

    Private Sub SpeedSlider_ValueChanged(sender As Object, e As Windows.RoutedPropertyChangedEventArgs(Of Double)) Handles speedSlider.ValueChanged
        savedSettings = False

        speedLabel.Text = My.Resources.Locale.Speed & String.Format("{0:0.00}", speedSlider.Value) & "):"
    End Sub

    Private Sub DropletNumberSlider_ValueChanged(sender As Object, e As Windows.RoutedPropertyChangedEventArgs(Of Double)) Handles dropletNumberSlider.ValueChanged
        savedSettings = False

        dropletNumberLabel.Text = My.Resources.Locale.DropletNumber & String.Format("{0:0}", dropletNumberSlider.Value) & "):"
    End Sub

    Private Sub WaveheightSlider_ValueChanged(sender As Object, e As Windows.RoutedPropertyChangedEventArgs(Of Double)) Handles waveheightSlider.ValueChanged
        savedSettings = False

        waveheightLabel.Text = My.Resources.Locale.Waveheight & String.Format("{0:0.00}", waveheightSlider.Value) & "):"
    End Sub

    Private Sub ResolutionSlider_ValueChanged(sender As Object, e As Windows.RoutedPropertyChangedEventArgs(Of Double)) Handles resolutionSlider.ValueChanged
        savedSettings = False

        resolutionLabel.Text = My.Resources.Locale.ResolutionRatio & String.Format("{0:0}", resolutionSlider.Value) & "):"
    End Sub

    Private Sub UpdateText()
        speedLabel.Text = My.Resources.Locale.Speed & String.Format("{0:0.00}", speedSlider.Value) & "):"
        dropletNumberLabel.Text = My.Resources.Locale.DropletNumber & String.Format("{0:0}", dropletNumberSlider.Value) & "):"
        waveheightLabel.Text = My.Resources.Locale.Waveheight & String.Format("{0:0.00}", waveheightSlider.Value) & "):"
        resolutionLabel.Text = My.Resources.Locale.ResolutionRatio & String.Format("{0:0}", resolutionSlider.Value) & "):"
    End Sub

    Private Sub LanguageBox_SelectionChanged(sender As Object, e As Windows.Controls.SelectionChangedEventArgs) Handles languageBox.SelectionChanged
        Dim stackPanel = DirectCast(DirectCast(languageBox.SelectedItem, Windows.Controls.ComboBoxItem).Content,
            Windows.Controls.StackPanel)

        Dim lang As String
        For Each child In stackPanel.Children.OfType(Of Windows.Controls.TextBlock)
            lang = child.Text
        Next

        My.Resources.Locale.Culture = New CultureInfo(lang)

        ' Prevent infinite loop
        If Not initialising Then
            languageChanged = True
            Program.ReloadWindow(languageBox.SelectedIndex) ' Reload window upon changing language
        End If
    End Sub
End Class

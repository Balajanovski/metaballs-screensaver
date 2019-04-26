Imports OpenTK
Imports OpenTK.Graphics
Imports OpenTK.Graphics.OpenGL4
Imports OpenTK.Input

Public Class Screensaver
    Inherits GameWindow

    Private time As Double

    Private Const waveHeight As Single = 2.1

    Private metaballsShader As Shader
    Private screenQuadRenderer As ScreenQuadRenderer
    Private dropletManager As DropletManager

    Public Sub New()
        MyBase.New(DisplayDevice.Default.Width, DisplayDevice.Default.Height,
                   GraphicsMode.Default,
                   "Screensaver",
                   GameWindowFlags.Default,
                   DisplayDevice.Default, 4, 4,
                   GraphicsContextFlags.Debug)
        VSync = VSyncMode.On
        prevMouseX = 0
        prevMouseY = 0
        time = 0
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        GL.ClearColor(0.0, 0.0, 0.0, 0.0)

        metaballsShader = New Shader("metaballs.vert", "metaballs.frag")
        screenQuadRenderer = New ScreenQuadRenderer()
        dropletManager = New DropletManager(30, New Vector3(0.0, 60.0, 40.0), 50.0, 60.0, 40.0, time)
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        GL.Viewport(0, 0, Width, Height)
    End Sub

    Private prevMouseX As Integer
    Private prevMouseY As Integer
    Protected Overrides Sub OnUpdateFrame(e As FrameEventArgs)
        MyBase.OnUpdateFrame(e)

        Dim cursorState = Mouse.GetCursorState

        ' If mouse is moved, close screensaver
        'If prevMouseX <> cursorState.X Or prevMouseY <> cursorState.Y _
        '       And prevMouseX <> 0 Or prevMouseY <> 0 Then
        '   [Exit]()
        'End If
        prevMouseX = cursorState.X
        prevMouseY = cursorState.Y

        ' If any key is pressed, close screensaver
        'HandleKeyboard()
    End Sub

    Private Sub HandleKeyboard()
        Dim keyState = Keyboard.GetState()

        If keyState.IsAnyKeyDown Then
            [Exit]()
        End If
    End Sub

    Protected Overrides Sub OnRenderFrame(e As FrameEventArgs)
        MyBase.OnRenderFrame(e)

        time += e.Time

        Dim resolution As Vector3 = New Vector3(DisplayDevice.Default.Width, DisplayDevice.Default.Height, 1.0)
        metaballsShader.Use()

        metaballsShader.SetVec3("iResolution", resolution)
        metaballsShader.SetFloat("iTime", time)
        metaballsShader.SetFloat("waveHeight", waveHeight)

        dropletManager.ApplyGravity(time, waveHeight)
        dropletManager.SendDataToShader(metaballsShader)

        screenQuadRenderer.Render()

        SwapBuffers()
    End Sub

End Class
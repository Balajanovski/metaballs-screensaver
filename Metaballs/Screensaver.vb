Imports OpenTK
Imports OpenTK.Graphics
Imports OpenTK.Graphics.OpenGL4
Imports OpenTK.Input

Imports System.Runtime.InteropServices
Imports System.Drawing

Public Class Screensaver
    Inherits GameWindow

    <DllImport("user32.dll")>
    Shared Function SetParent(hWndChild As IntPtr, hWndNewParent As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Shared Function SetWindowLong(hWnd As IntPtr, nIndex As Integer, dwNewLong As IntPtr) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Shared Function GetWindowLong(hWnd As IntPtr, nIndex As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Shared Function GetClientRect(hWnd As IntPtr, ByRef lpRect As Rectangle) As Boolean
    End Function

    Private time As Double

    Private metaballsShader As Shader
    Private screenQuadRenderer As ScreenQuadRenderer
    Private dropletManager As DropletManager
    Private offscreenRenderBuffer As OffscreenRenderBuffer
    Private textureBlitShader As Shader

    Private screensaverPreviewMode As Boolean

    Public Sub New(w As Integer, h As Integer,
                   previewMode As Boolean,
                   previewWinHandle As IntPtr,
                   Optional gameWindowFlags As GameWindowFlags = GameWindowFlags.Default)

        MyBase.New(w, h,
                   GraphicsMode.Default,
                   "Screensaver",
                   gameWindowFlags,
                   DisplayDevice.Default, 4, 4,
                   GraphicsContextFlags.Default)
        VSync = VSyncMode.On
        time = 0
        screensaverPreviewMode = previewMode
        WindowBorder = WindowBorder.Hidden

        If gameWindowFlags = GameWindowFlags.Fullscreen Then
            CursorVisible = False
        End If

        If previewMode And previewWinHandle <> IntPtr.Zero Then
            PreparePreviewMode(previewWinHandle)
        End If
    End Sub

    Private Sub PreparePreviewMode(previewWinHandle As IntPtr)
        ' Set the preview window as the parent of this window
        SetParent(WindowInfo.Handle, previewWinHandle)

        ' Make this a child window so that it will close when the parent dialog closes
        ' GWL_STYLE = -16, WS_CHILD = 0x40000000
        SetWindowLong(WindowInfo.Handle, -16, New IntPtr(GetWindowLong(WindowInfo.Handle, -16) Or &H40000000))

        ' Place our window inside the parent
        Dim parentRectangle As Rectangle
        GetClientRect(previewWinHandle, parentRectangle)
        Width = parentRectangle.Width
        Height = parentRectangle.Height
        Size = parentRectangle.Size
        Location = New Point(0, 0)
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        GL.ClearColor(0.0, 0.0, 0.0, 0.0)

        metaballsShader = New Shader("ScreenQuadRenderer.vert", "metaballs.frag")
        screenQuadRenderer = New ScreenQuadRenderer()
        dropletManager = New DropletManager(ConfigManager.Instance.DropletNumber, New Vector3(0.0, 60.0, 40.0), 50.0, 60.0, 40.0, time)
        offscreenRenderBuffer = New OffscreenRenderBuffer(Width * ConfigManager.Instance.ResolutionRatio,
                                                          Height * ConfigManager.Instance.ResolutionRatio)
        textureBlitShader = New Shader("ScreenQuadRenderer.vert", "BlitTextureToScreen.frag")

        mouseLocInitialized = False
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        GL.Viewport(0, 0, Width, Height)
    End Sub

    Private mouseLoc As New Vector2()
    Dim mouseLocInitialized As Boolean = False
    Protected Overrides Sub OnUpdateFrame(e As FrameEventArgs)
        MyBase.OnUpdateFrame(e)

        Dim cursorState = Mouse.GetCursorState

        ' If mouse is moved, close screensaver
        If (Math.Abs(mouseLoc.X - cursorState.X) > 5 Or Math.Abs(mouseLoc.Y - cursorState.Y) > 5) _
                   And mouseLocInitialized And Not screensaverPreviewMode Then
            [Exit]()
        End If
        mouseLoc.X = cursorState.X
        mouseLoc.Y = cursorState.Y
        mouseLocInitialized = True

        ' If any key is pressed, close screensaver
        HandleKeyboard()
    End Sub

    Private Sub HandleKeyboard()
        Dim keyState = Keyboard.GetState()

        If keyState.IsAnyKeyDown Then
            [Exit]()
        End If
    End Sub

    Protected Overrides Sub OnRenderFrame(e As FrameEventArgs)
        MyBase.OnRenderFrame(e)

        time += e.Time * ConfigManager.Instance.Speed

        ' Draw metaballs to offscreen buffer
        Dim resolution As Vector3 = New Vector3(Width * ConfigManager.Instance.ResolutionRatio,
                                                Height * ConfigManager.Instance.ResolutionRatio, 1.0)
        metaballsShader.Use()

        metaballsShader.SetVec3("iResolution", resolution)
        metaballsShader.SetFloat("iTime", time)
        metaballsShader.SetFloat("waveHeight", ConfigManager.Instance.WaveHeight)
        metaballsShader.SetInt("NUM_DROPLETS", ConfigManager.Instance.DropletNumber)

        dropletManager.ApplyGravity(time, ConfigManager.Instance.WaveHeight)
        dropletManager.SendDataToShader(metaballsShader)

        offscreenRenderBuffer.Bind()
        screenQuadRenderer.Render()
        offscreenRenderBuffer.UnBind()

        ' Draw the scaled offscreen buffer to the screen
        ' This is done so that the resolution of the screensaver may be adjusted
        textureBlitShader.Use()
        textureBlitShader.SetInt("textureToDraw", 0)
        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture2D, offscreenRenderBuffer.currentFrame)

        screenQuadRenderer.Render()

        SwapBuffers()
    End Sub

End Class
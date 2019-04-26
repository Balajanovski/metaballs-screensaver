Imports OpenTK
Imports System.Collections.Generic

Public Class Droplet
    Public position As Vector3
    Public velocity As Single ' Velocity only has a y component, as the droplets can only fall down
    Public radius As Single

    Public Sub New(pos As Vector3, vel As Single, rad As Single)
        position = pos
        velocity = vel
        radius = rad
    End Sub
End Class

Public Class DropletManager
    Private droplets As New List(Of Droplet)

    Private Const GRAVITY As Single = 2.1

    Private prevUpdateTime As Single

    Private numberOfDroplets As Integer

    Private spawnBoxWidth As Single
    Private spawnBoxLength As Single
    Private spawnBoxHeight As Single
    Private spawnBoxCentre As Vector3

    Public Sub New(numDroplets As Integer,
                   spwnBoxCentre As Vector3,
                   spwnBoxWidth As Single,
                   spwnBoxHeight As Single,
                   spwnBoxLength As Single,
                   startTime As Single)
        prevUpdateTime = startTime
        numberOfDroplets = numDroplets

        spawnBoxWidth = spwnBoxWidth
        spawnBoxLength = spwnBoxLength
        spawnBoxHeight = spwnBoxHeight
        spawnBoxCentre = spwnBoxCentre

        For i = 0 To numDroplets - 1
            Dim x As Single = Math.Abs(RandomFloatGenerator.instance().NextFloat() Mod spawnBoxWidth) - (0.5 * spawnBoxWidth) + spawnBoxCentre.X
            Dim y As Single = -(Math.Abs(RandomFloatGenerator.instance().NextFloat() Mod spawnBoxHeight) - (0.5 * spawnBoxHeight) + spawnBoxCentre.Y)
            Dim z As Single = Math.Abs(RandomFloatGenerator.instance().NextFloat() Mod spawnBoxLength) - (0.5 * spawnBoxLength) + spawnBoxCentre.Z
            Dim radius As Single = RandomFloatGenerator.instance().NextFloat() Mod 0.5 + 0.75

            droplets.Add(New Droplet(New Vector3(x, y, z), 0.0, radius))
        Next
    End Sub

    Public Sub ApplyGravity(currentTime As Single, waveHeight As Single)
        Dim t As Single = currentTime - prevUpdateTime
        For i = 0 To droplets.Count() - 1
            ' Apply kinematics equation: s = ut + 0.5*at^2
            Dim displacement = (droplets(i).velocity * t) + (0.5 * GRAVITY * (t * t))
            droplets(i).position += New Vector3(0.0, displacement, 0.0)

            ' Apply kinematics equation: v = u + at
            droplets(i).velocity += (GRAVITY * t)

            If (droplets(i).position.Y > waveHeight) Then
                droplets(i).velocity = 0.0

                Dim x As Single = Math.Abs(RandomFloatGenerator.instance().NextFloat() Mod spawnBoxWidth) - (0.5 * spawnBoxWidth) + spawnBoxCentre.X
                Dim y As Single = -(Math.Abs(RandomFloatGenerator.instance().NextFloat() Mod spawnBoxLength) - (0.5 * spawnBoxHeight) + spawnBoxCentre.Y)
                Dim z As Single = Math.Abs(RandomFloatGenerator.instance().NextFloat() Mod spawnBoxLength) - (0.5 * spawnBoxLength) + spawnBoxCentre.Z

                droplets(i).position = New Vector3(x, y, z)
            End If

        Next

        prevUpdateTime = currentTime
    End Sub

    Public Sub SendDataToShader(ByRef shader As Shader)
        shader.SetVec3Array("dropletCenters", numberOfDroplets, droplets.Select(Function(t) t.position).ToArray())
        shader.SetFloatArray("dropletRadii", numberOfDroplets, droplets.Select(Function(t) t.radius).ToArray())
    End Sub
End Class

Imports System.Threading

Public Class FormMain
    Private areaSize As Integer
    Private areaMargin As Integer = 60
    Private order As Integer = 1 ' Hilbert's Order
    Private n As Integer
    Private grid As Integer
    Private indexes As New List(Of Integer)
    Private points As New List(Of Point)
    Private curveColor As New HLSRGB(Color.Red)
    Private colorMode As Boolean
    Private screenTooSmall As Boolean

    Private testPoint As New Point(0, 0)
    Private showTestPoint As Boolean = True

    Private stepByStepThread As Thread
    Private stepByStepEnable As Boolean
    Private stepByStepIndex As Integer
    Private stepByStepDelay As Integer = 35

    Private isLeftMouseButtonDown As Boolean

    Private Sub FormMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint, True)
        Me.SetStyle(ControlStyles.OptimizedDoubleBuffer, True)
        Me.SetStyle(ControlStyles.UserPaint, True)
        Me.SetStyle(ControlStyles.ResizeRedraw, True)

        AddHandler Me.KeyDown, Sub(s1 As Object, e1 As KeyEventArgs)
                                   Select Case e1.KeyCode
                                       Case Keys.Down
                                           If order > 0 Then
                                               order -= 1
                                               BuildCurve()
                                           End If
                                       Case Keys.Up
                                           order += 1
                                           BuildCurve()
                                       Case Keys.Enter
                                           stepByStepEnable = Not stepByStepEnable
                                           If Not stepByStepEnable Then
                                               stepByStepIndex = 0
                                               Me.Invalidate()
                                           End If
                                       Case Keys.T
                                           showTestPoint = Not showTestPoint
                                           If Not stepByStepEnable Then Me.Invalidate()
                                       Case Keys.C
                                           colorMode = Not colorMode
                                           BuildCurve()
                                   End Select
                               End Sub
        AddHandler Me.MouseDown, Sub(s1 As Object, e1 As MouseEventArgs)
                                     If e1.Button = MouseButtons.Left Then
                                         isLeftMouseButtonDown = True
                                         SetTestPointFromMouse(e1.Location)
                                     End If
                                 End Sub
        AddHandler Me.MouseMove, Sub(s1 As Object, e1 As MouseEventArgs) If isLeftMouseButtonDown Then SetTestPointFromMouse(e1.Location)
        AddHandler Me.MouseUp, Sub(s1 As Object, e1 As MouseEventArgs) isLeftMouseButtonDown = Not (e1.Button = MouseButtons.Left)
        AddHandler Me.Resize, Sub() BuildCurve()
        BuildCurve()

        stepByStepThread = New Thread(Sub()
                                          Dim d As Integer = 1
                                          Do
                                              If stepByStepEnable Then
                                                  If stepByStepIndex >= points.Count - 2 OrElse stepByStepIndex < 0 Then
                                                      d = -d
                                                      Thread.Sleep(stepByStepDelay)
                                                  End If
                                                  stepByStepIndex += d
                                                  Me.Invalidate()
                                                  Thread.Sleep(stepByStepDelay)
                                              ElseIf colorMode Then
                                                  curveColor.Hue += 1.0
                                                  Me.Invalidate()
                                                  Thread.Sleep(30)
                                              End If
                                          Loop
                                      End Sub)
        stepByStepThread.IsBackground = True
        stepByStepThread.Start()
    End Sub

    Private Sub SetTestPointFromMouse(mouseLocation As Point)
        testPoint = mouseLocation
        testPoint.Y = Me.DisplayRectangle.Height - testPoint.Y
        testPoint.Offset(-(Me.DisplayRectangle.Width - areaSize - areaMargin / 2), -(Me.DisplayRectangle.Height - areaSize) / 2)
        Me.Invalidate()
    End Sub

    Private Sub BuildCurve()
        n = 2 ^ order
        areaSize = Math.Min(Me.DisplayRectangle.Width - areaMargin, Me.DisplayRectangle.Height - areaMargin)
        areaSize -= areaSize Mod n
        grid = areaSize \ n
        screenTooSmall = (grid = 0)

        indexes.Clear()
        For y As Integer = 0 To n - 1
            For x As Integer = 0 To n - 1
                indexes.Add(Hilbert.PointToIndex(n, x, y))
            Next
        Next
        indexes.Sort()

        points.Clear()
        Dim p As Point
        For Each index As Integer In indexes
            p = Hilbert.IndexToPoint(n, index)
            p.X = p.X * grid + grid / 2
            p.Y = p.Y * grid + grid / 2
            points.Add(p)
        Next

        stepByStepIndex = 0
        stepByStepDelay = 10000 / points.Count
        If stepByStepDelay = 0 Then
            stepByStepDelay = 1
        ElseIf stepByStepDelay > 500 Then
            stepByStepDelay = 500
        End If

        Me.Invalidate()
    End Sub

    Private Function ContainsIndex(hi As Integer) As Boolean
        For i As Integer = 0 To indexes.Count - 1
            If indexes(i) = hi Then Return True
        Next
        Return False
    End Function

    Private Sub FormMain_Paint(sender As Object, e As PaintEventArgs) Handles Me.Paint
        Dim g As Graphics = e.Graphics

        g.Clear(Color.Black)
        g.SmoothingMode = Drawing2D.SmoothingMode.None
        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.None

        ' Make (0,0) the lower/left corner (instead of the usual top/left)
        g.ScaleTransform(1.0, -1.0)
        g.TranslateTransform(0, -Me.DisplayRectangle.Height)

        ' Center the grid and the curve
        g.TranslateTransform(Me.DisplayRectangle.Width - areaSize - areaMargin / 2, (Me.DisplayRectangle.Height - areaSize) / 2)

        Using pg As New Pen(Color.FromArgb(96, Color.DimGray))
            If areaSize > 0 AndAlso grid > 0 Then
                For y As Integer = grid To areaSize - grid Step grid
                    g.DrawLine(pg, 0, y, areaSize, y)
                Next
                For x As Integer = grid To areaSize - grid Step grid
                    g.DrawLine(pg, x, 0, x, areaSize)
                Next
                g.DrawRectangle(Pens.DimGray, 0, 0, areaSize, areaSize)
            End If
        End Using

        If showTestPoint Then g.FillEllipse(Brushes.Red, New Rectangle(testPoint.X - 5, testPoint.Y - 5, 10, 10))

        ' Render Hilbert's Curve
        If points.Count > 1 Then
            If stepByStepEnable Then
                ' FIXME: This is extremely inefficient, because without using a persistent surface (such as a DirectBitmap), we need to draw
                '   all lines from 0 to stepByStepIndex, every single time
                Dim h As Double = curveColor.Hue
                For i As Integer = 0 To stepByStepIndex
                    If colorMode Then
                        DrawGradientLine(g, points(i), points(i + 1))
                    Else
                        g.DrawLine(Pens.White, points(i), points(i + 1))
                    End If
                Next
                curveColor.Hue = h + 1.0
            Else
                Using p As New Pen(If(colorMode, curveColor, Color.White))
                    g.DrawLines(p, points.ToArray())
                End Using
            End If
        End If

        ' Find the closest point in the curve to 'testPoint'
        If showTestPoint AndAlso points.Count > 1 Then
            Dim minD As Double = Double.MaxValue
            Dim d As Double
            Dim p As Point
            Dim bestPoint As Point
            Dim bestIndex As Integer
            For i As Integer = 0 To points.Count - 2
                p = ClosestPointInLineToAPoint(testPoint, points(i), points(i + 1))
                d = DistanceFromPointToPoint(testPoint, p)
                If d < minD Then
                    bestPoint = p
                    bestIndex = i
                    minD = d
                End If
            Next
            g.FillEllipse(Brushes.Magenta, New Rectangle(bestPoint.X - 5, bestPoint.Y - 5, 10, 10))

            PrintHilbertOrder(g)
            g.DrawString($"Distance: {minD:N2}", Me.Font, Brushes.LightGray, 10, 10 + 2 * Me.Font.Height)
            g.DrawString($"Index:    {bestIndex:N0}/{points.Count - 1:N0}", Me.Font, Brushes.LightGray, 10, 10 + 3 * Me.Font.Height)
        Else
            PrintHilbertOrder(g)
        End If

        PrintHelp(g)
    End Sub

    Private Sub PrintHilbertOrder(g As Graphics)
        g.ResetTransform()
        g.DrawString($"Order:    {order}", Me.Font, Brushes.White, 10, 10)
    End Sub

    Private Sub PrintHelp(g As Graphics)
        g.DrawString("[Up]    Increase Order", Me.Font, Brushes.Gray, 10, 10 + 5 * Me.Font.Height)
        g.DrawString("[Down]  Decrease Order", Me.Font, Brushes.Gray, 10, 10 + 6 * Me.Font.Height)
        g.DrawString("[ENTER] Toggle animation", Me.Font, If(stepByStepEnable, Brushes.OrangeRed, Brushes.Gray), 10, 10 + 7 * Me.Font.Height)
        g.DrawString("[T]     Toggle test point", Me.Font, If(showTestPoint, Brushes.OrangeRed, Brushes.Gray), 10, 10 + 8 * Me.Font.Height)
        g.DrawString("[C]     Toggle color mode", Me.Font, If(colorMode, Brushes.OrangeRed, Brushes.Gray), 10, 10 + 9 * Me.Font.Height)

        If screenTooSmall Then
            g.DrawString("<< Screen too small >>", Me.Font, Brushes.Red, 10, 10 + 11 * Me.Font.Height)
        End If
    End Sub

    Private Sub DrawGradientLine(g As Graphics, p1 As Point, p2 As Point)
        If p1.X = p2.X Then ' Vertical Lines
            For y As Integer = p1.Y To p2.Y Step If(p2.Y > p1.Y, 1, -1)
                Using p As New Pen(curveColor)
                    g.DrawLine(p, p1.X, p1.Y, p2.X, y)
                End Using
                curveColor.Hue += 0.05
            Next
            Exit Sub
        ElseIf p1.Y = p2.Y Then ' Horizontal Lines
            For x As Integer = p1.X To p2.X Step If(p2.X > p1.X, 1, -1)
                Using p As New Pen(curveColor)
                    g.DrawLine(p, p1.X, p1.Y, x, p2.Y)
                End Using
                curveColor.Hue += 0.05
            Next
            Exit Sub
        End If

        ' Lines at an angle
        Dim c As Point = p1
        Dim a As Double = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X)
        Dim d As Integer = DistanceFromPointToPoint(p1, p2)

        For r As Integer = 0 To d
            p2.X = c.X + r * Math.Cos(a)
            p2.Y = c.Y + r * Math.Sin(a)

            If p1 <> p2 Then
                Using p As New Pen(curveColor)
                    g.DrawLine(p, p1, p2)
                End Using

                curveColor.Hue += 0.05
                p2 = p1
            End If
        Next
    End Sub

    Private Function DistanceFromPointToPoint(p1 As Point, p2 As Point) As Double
        Dim dx As Integer = p1.X - p2.X
        Dim dy As Integer = p1.Y - p2.Y
        Return Math.Sqrt(dx * dx + dy * dy)
    End Function

    Private Function ClosestPointInLineToAPoint(p As Point, A As Point, B As Point) As Point
        'Dim dx As Integer = A.X - B.X
        'Dim dy As Integer = A.Y - B.Y
        'Dim det As Integer = A.Y * B.X - A.X * B.Y
        'Dim dot As Integer = dx * p.X + dy * p.Y
        'Dim x As Integer = dot * dx + det * dy
        'Dim y As Integer = dot * dy - det * dx
        'Dim z As Integer = dx * dx + dy * dy
        'Dim zinv As Double = 1 / z
        'Return New Point(x * zinv, y * zinv)

        Dim AP As New Point(p.X - A.X, p.Y - A.Y)
        Dim AB As New Point(B.X - A.X, B.Y - A.Y)
        Dim AB2 As Integer = AB.X * AB.X + AB.Y * AB.Y
        Dim dot As Integer = AP.X * AB.X + AP.Y * AB.Y
        Dim t As Double = dot / AB2

        If t >= 0.0 AndAlso t <= 1.0 Then
            Return New Point(A.X + AB.X * t, A.Y + AB.Y * t)
        Else
            If DistanceFromPointToPoint(p, A) < DistanceFromPointToPoint(p, B) Then
                Return A
            Else
                Return B
            End If
        End If
    End Function
End Class
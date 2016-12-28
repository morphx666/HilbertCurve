'https://en.wikipedia.org/wiki/Hilbert_curve
Public Class Hilbert
    Public Shared Function PointToIndex(n As Integer, x As Integer, y As Integer) As Integer
        Return PointToIndex(n, New Point(x, y))
    End Function

    Public Shared Function PointToIndex(n As Integer, p As Point) As Integer
        Dim s As Integer = n \ 2
        Dim rx As Integer
        Dim ry As Integer
        Dim index As Integer = 0
        While s > 0
            rx = If((p.X And s) > 0, 1, 0)
            ry = If((p.Y And s) > 0, 1, 0)
            index += s * s * ((3 * rx) Xor ry)
            p = Rotate(s, p, rx, ry)
            s \= 2
        End While
        Return index
    End Function

    Public Shared Function IndexToPoint(n As Integer, index As Integer) As Point
        Dim p As New Point(0, 0)
        Dim rx As Integer
        Dim ry As Integer
        Dim s As Integer = 1
        Dim t As Integer = index
        While s < n
            rx = 1 And (t \ 2)
            ry = 1 And (t Xor rx)
            p = Rotate(s, p, rx, ry)
            p.X += s * rx
            p.Y += s * ry
            t \= 4
            s *= 2
        End While
        Return p
    End Function

    Public Shared Function IndexToTransformedPoint(n As Integer, index As Integer, w As Integer) As Point
        Dim p As Point = IndexToPoint(n, index)
        Dim g As Integer = w \ n
        Return New Point(p.X * g + g / 2, p.Y * g + g / 2)
    End Function

    Private Shared Function Rotate(n As Integer, p As Point, rx As Integer, ry As Integer) As Point
        If ry = 0 Then
            If rx = 1 Then
                p.X = (n - 1) - p.X
                p.Y = (n - 1) - p.Y
            End If

            Dim tmp As Integer = p.X
            p.X = p.Y
            p.Y = tmp
        End If
        Return p
    End Function
End Class

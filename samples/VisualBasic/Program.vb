Option Strict On

Imports SampleVb
Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Text

<Assembly: AssemblyMetadata("Hello", SampleVb.AssemblyMeta.Strings.Hello)>

Module Program
    Function Main() As Integer
        Dim failures As New List(Of String)
        Check(failures, "Namespace", If(GetType(AssemblyMeta).Namespace, ""), "SampleVb")
        Check(failures, "Info.Company", AssemblyMeta.Info.Company, "Skymly")
        Check(failures, "Info.Title", AssemblyMeta.Info.Title, "AssemblyMeta 示例标题")
        Check(failures, "Info.Product", AssemblyMeta.Info.Product, "AssemblyMeta 示例")
        Check(failures, "Info.Version", AssemblyMeta.Info.Version, "1.2.3.0")
        Check(failures, "Info.Description", AssemblyMeta.Info.Description, "第一行" & vbLf & "第二行 ""引号"" ; 分号 # 井号")
        Check(failures, "Constants.Banner", AssemblyMeta.Constants.Banner, "line1" & vbLf & "line2")
        Check(failures, "Constants.Weird", AssemblyMeta.Constants.Weird, "a;b#c ""q""")
        Check(failures, "Constants.TimeoutSeconds", AssemblyMeta.Constants.Http.TimeoutSeconds.ToString(), "30")
        Check(failures, "Project.AssemblyName", AssemblyMeta.Project.AssemblyName, "VisualBasic")
        Check(failures, "Metadata.DemoKey", AssemblyMeta.Metadata.DemoKey, "a;b#c")
        If AssemblyMeta.Git.Commit.Length = 0 OrElse AssemblyMeta.Git.Commit = "[pending build]" Then
            failures.Add("Git.Commit 未在真实构建中填充")
        End If
        If Not AssemblyMeta.Git.Commit.StartsWith(AssemblyMeta.Git.Sha, StringComparison.Ordinal) Then
            failures.Add("Git.Sha 不是 Commit 的前缀")
        End If
        If AssemblyMeta.Git.Root.Contains("\"c) Then
            failures.Add("Git.Root 仍包含反斜杠: " & AssemblyMeta.Git.Root)
        End If

        Dim note = AssemblyMeta.Resources.Content.Note.Text
        Check(failures, "Resources.Note", note, "仅 Resources 包。" & vbLf)
        If Not Object.ReferenceEquals(note, AssemblyMeta.Resources.Content.Note.Text) Then
            failures.Add("Resources.Note.Text 没有缓存")
        End If
        Check(failures, "Resources.Note.GetBytes", Encoding.UTF8.GetString(AssemblyMeta.Resources.Content.Note.GetBytes()), note)
        Using stream = AssemblyMeta.Resources.Content.Note.GetStream()
            Using copy As New IO.MemoryStream()
                stream.CopyTo(copy)
                Check(failures, "Resources.Note.GetStream", Encoding.UTF8.GetString(copy.ToArray()), note)
            End Using
        End Using

        Check(failures, "Strings.Hello", AssemblyMeta.Strings.Hello, "你好")
        Check(failures, "Strings.Greeting", AssemblyMeta.Strings.Greeting, "你好，{0}")
        Dim hello = GetType(Program).Assembly.GetCustomAttributes(Of AssemblyMetadataAttribute)().FirstOrDefault(Function(attribute) attribute.Key = "Hello")
        Check(failures, "StringHello", If(hello Is Nothing, "", hello.Value), "你好")
        Dim stringsType = GetType(AssemblyMeta).GetNestedType("Strings")
        If stringsType Is Nothing Then
            failures.Add("缺少 Strings")
        ElseIf stringsType.GetField("Skip", BindingFlags.Public Or BindingFlags.Static) IsNot Nothing OrElse stringsType.GetNestedType("Fr") IsNot Nothing Then
            failures.Add("卫星 resx 进入了 Strings")
        End If

        If failures.Count > 0 Then
            For Each failure In failures
                Console.Error.WriteLine(failure)
            Next
            Return 1
        End If

        Console.WriteLine("Visual Basic 样例断言通过。")
        Return 0
    End Function

    Private Sub Check(failures As List(Of String), name As String, actual As String, expected As String)
        If Not String.Equals(actual, expected, StringComparison.Ordinal) Then
            failures.Add(name & ": 期望 '" & expected & "'，实际 '" & actual & "'")
        End If
    End Sub
End Module
﻿Option Strict On

Public Class frmMain
    Private isDirty As Boolean = True
    Private strPreviousOutput As String = ""
    Private strPreviousVVPInput As String = ""
    Private currDirectory As String = ""

    Private Sub AddFile()
        Dim filesAdded As Integer = 0

        Dim dlgOpen As New OpenFileDialog

        dlgOpen.CheckFileExists = True
        dlgOpen.DefaultExt = "v"
        dlgOpen.Filter = "Verilog Code (*.v)|*.v|All files (*.*)|*.*"
        dlgOpen.Multiselect = True

        If dlgOpen.ShowDialog = Windows.Forms.DialogResult.OK Then

            ' Iterate through each opened file
            For Each file As String In dlgOpen.FileNames

                ' Add files to list box
                If lstFiles.Items.IndexOf(file) < 0 Then
                    lstFiles.Items.Add(file)
                    filesAdded += 1
                    isDirty = True
                End If

                ' Add previous files to address bar.
                If My.Settings.Files.IndexOf(file) < 0 Then

                    ' Number of files on settings doesn't exceed 20 :)
                    If My.Settings.Files.Count >= 20 Then
                        My.Settings.Files.RemoveAt(0)
                    End If

                    ' Add more.
                    My.Settings.Files.Add(file)

                    ' Save and apply changes
                    My.Settings.Save()
                End If
            Next
        End If



        lblStatus.Text = "Files added: " & filesAdded & ". Total files: " & lstFiles.Items.Count & "."

    End Sub

    Private Function Exists(ByVal file As String) As Boolean
        If file.Length > 0 Then
            If My.Computer.FileSystem.FileExists(file) Then
                Return True
            End If
        End If

        Return False
    End Function

    Private Sub frmMain_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        lblStatus.Text = "Ready"

        If My.Settings.Files Is Nothing Then
            My.Settings.Files = New Specialized.StringCollection
        End If
    End Sub

    Private Sub UpdateCurrentDirectory()
        If Not isWorkingDirectorySet() Then
            ChangeCurrentDirectory()
        End If
    End Sub

    Private Sub ChangeCurrentDirectory()
        Dim dlgWork As New FolderBrowserDialog
        dlgWork.Description = "Please set a working directory. This directory must exist and is writable. This will become your project directory."
        dlgWork.ShowNewFolderButton = True

        If dlgWork.ShowDialog = Windows.Forms.DialogResult.OK Then
            currDirectory = dlgWork.SelectedPath
            ChDir(currDirectory)
            Dim drive As String = System.IO.Path.GetPathRoot(currDirectory)
            ChDrive(drive(0))
        End If
    End Sub

    Private Function isWorkingDirectorySet() As Boolean
        If Not My.Computer.FileSystem.DirectoryExists(currDirectory) Then
            Return False
        Else
            Return True
        End If
    End Function

    Private Function InitiateCheckWorkingDirectory() As Boolean
        If isWorkingDirectorySet() Then
            Return True
        Else
            Return False
            Beep()
            lblStatus.Text = "You have not yet set the working directory."
        End If
    End Function

    Private Sub InitiateCompile()
        UpdateCurrentDirectory()

        If Not InitiateCheckWorkingDirectory() Then
            Return
        End If

        If lstFiles.Items.Count > 0 Then
            Dim dlgSave As New SaveFileDialog
            dlgSave.DefaultExt = "vvp"
            dlgSave.Filter = "iVerilog Compiled Code (*.vvp)|*.vvp|All files (*.*)|*.*"
            dlgSave.OverwritePrompt = True
            dlgSave.InitialDirectory = currDirectory

            If Not isDirty Then
                If MsgBox("Overwrite previous compilation?", CType(MsgBoxStyle.YesNo + MsgBoxStyle.Question, MsgBoxStyle), "Overwrite previous?") = MsgBoxResult.Yes Then
                    Compile(strPreviousOutput)
                    isDirty = False
                    Return
                Else
                    isDirty = True
                End If
            End If

            If isDirty Then
                If dlgSave.ShowDialog = Windows.Forms.DialogResult.OK Then
                    Compile(dlgSave.FileName)
                    isDirty = False
                End If
            End If

        End If
    End Sub

    Private Sub InitiateSimulation()
        UpdateCurrentDirectory()

        If Not InitiateCheckWorkingDirectory() Then
            Return
        End If

        If isDirty = False Then
            If My.Computer.FileSystem.FileExists(strPreviousOutput) Then
                Simulate(strPreviousOutput)

            End If
        Else
            Dim dlgOpen As New OpenFileDialog

            dlgOpen.CheckFileExists = True
            dlgOpen.DefaultExt = "vvp"
            dlgOpen.Filter = "iVerilog Compiled Code (*.vvp)|*.vvp|All files (*.*)|*.*"
            dlgOpen.Multiselect = False
            dlgOpen.InitialDirectory = currDirectory

            If dlgOpen.ShowDialog = Windows.Forms.DialogResult.OK Then
                Simulate(dlgOpen.FileName)
            End If
        End If
    End Sub

    Private Sub InitiateShowWave()
        UpdateCurrentDirectory()

        If Not InitiateCheckWorkingDirectory() Then
            Return
        End If

        Dim dlgOpen As New OpenFileDialog

        dlgOpen.CheckFileExists = True
        dlgOpen.DefaultExt = "dump"
        dlgOpen.Filter = "GTKWave Wave Dump (*.dump)|*.dump|All files (*.*)|*.*"
        dlgOpen.Multiselect = False
        dlgOpen.InitialDirectory = currDirectory

        If dlgOpen.ShowDialog = Windows.Forms.DialogResult.OK Then
            ShowWave(dlgOpen.FileName)
        End If
    End Sub

    Private Sub Compile(ByVal strOutput As String)
        strPreviousOutput = strOutput

        Dim strFiles As String = ""

        For Each file As String In lstFiles.Items
            strFiles += """" & file & """ "
        Next

        Dim strCommand As String = "iVerilog -o """ & strOutput & """ " & strFiles
        Debug.WriteLine(strCommand)

        Dim proc As Integer = Shell(strCommand, AppWinStyle.NormalFocus, False)
        lblStatus.Text = "Compiler process ID " & proc & "."
    End Sub

    Private Sub Simulate(ByVal VVPInput As String)
        strPreviousVVPInput = VVPInput

        Dim strCommand As String = "vvp """ & VVPInput & """ -vcd"
        Debug.WriteLine(strCommand)

        Dim proc As Integer = Shell(strCommand, AppWinStyle.NormalFocus, False)
        lblStatus.Text = "Simulator process ID " & proc & "."
    End Sub

    Private Sub ShowWave(ByVal DumpInput As String)
        strPreviousVVPInput = DumpInput

        Dim strCommand As String = "gtkwave """ & DumpInput & """"
        Debug.WriteLine(strCommand)

        Dim proc As Integer = Shell(strCommand, AppWinStyle.NormalFocus, False)
        lblStatus.Text = "GTKWave process ID " & proc & "."
    End Sub

    Private Sub Edit()
        If Not Exists(My.Settings.Editor) Then
            Dim dlgOpen As New OpenFileDialog

            dlgOpen.Title = "Find a text editor."
            dlgOpen.CheckFileExists = True
            dlgOpen.DefaultExt = "exe"
            dlgOpen.Filter = "Executable Program (*.exe)|*.exe|All files (*.*)|*.*"
            dlgOpen.Multiselect = False

            If dlgOpen.ShowDialog = Windows.Forms.DialogResult.OK Then
                My.Settings.Editor = dlgOpen.FileName
                My.Settings.Save()
            End If
        End If

        If lstFiles.SelectedItems.Count > 0 Then
            If Exists(My.Settings.Editor) Then
                Dim strCommand As String = My.Settings.Editor & " " & lstFiles.SelectedItem.ToString
                Debug.WriteLine(strCommand)

                Dim proc As Integer = Shell(strCommand, AppWinStyle.NormalFocus, False)
                lblStatus.Text = "Editor process ID " & proc & "."
            End If
        End If
    End Sub

    Private Sub Remove()
        Dim removedItems As Integer = 0

        While lstFiles.SelectedItems.Count > 0
            lstFiles.Items.Remove(lstFiles.SelectedItem)
            removedItems += 1
        End While

        lblStatus.Text = "Files removed: " & removedItems & ". Total files: " & lstFiles.Items.Count & "."
    End Sub

    Private Sub frmMain_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown
        UpdateCurrentDirectory()
    End Sub

    Private Sub AboutToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AboutToolStripMenuItem.Click

        If frmAbout.Visible = False Then
            frmAbout.StartPosition = FormStartPosition.CenterScreen
            frmAbout.Show()
        End If
    End Sub

    Private Sub RemoveToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RemoveToolStripMenuItem.Click
        Remove()
    End Sub

    Private Sub EditeFileToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles EditeFileToolStripMenuItem.Click
        Edit()
    End Sub

    Private Sub AddFileToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AddFileToolStripMenuItem.Click
        AddFile()
    End Sub

    Private Sub AddFilesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AddFilesToolStripMenuItem.Click
        AddFile()
    End Sub

    Private Sub CompileToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CompileToolStripMenuItem.Click
        InitiateCompile()
    End Sub

    Private Sub SimulateToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SimulateToolStripMenuItem.Click
        InitiateSimulation()
    End Sub

    Private Sub ShowWaveToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ShowWaveToolStripMenuItem.Click
        InitiateShowWave()
    End Sub

    Private Sub ChangeWorkingDirectoryToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ChangeWorkingDirectoryToolStripMenuItem.Click
        ChangeCurrentDirectory()
    End Sub

    Private Sub NewProjectToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NewProjectToolStripMenuItem.Click
        InitiateNewProject()
    End Sub

    Private Sub InitiateNewProject()
        lstFiles.Items.Clear()
        ChangeCurrentDirectory()
    End Sub

    Private Sub ExitToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ExitToolStripMenuItem.Click
        Application.Exit()
    End Sub

    Private Sub OpenProjectToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OpenProjectToolStripMenuItem.Click
        DeveloperNotice()
    End Sub

    Private Sub DeveloperNotice()
        MsgBox("This feature is not yet implemented in this version of jVerilog.", CType(vbInformation + MsgBoxStyle.OkOnly, MsgBoxStyle), "Developer Notice")
    End Sub

    Private Sub NewFileToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NewFileToolStripMenuItem.Click
        DeveloperNotice()
    End Sub

    Private Sub HelpToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles HelpToolStripMenuItem1.Click
        DeveloperNotice()
    End Sub
End Class

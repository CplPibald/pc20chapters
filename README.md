# pc20chapters
Winforms-based app for generating Podcast-2.0 compliant chapters file

Creates a series of text boxes into which you can put your podcast chapter data.
When you click "Save", it emits a .json file which you can publish with your RSS feed.

Requires Powershell v5.0 or above (this is the default on Windows 10)

##Enable running PS scripts:

Microsoft ships both PowerShell and Windows Forms in every version of Windows.  But they make it difficult to run them.
Microsoft disables running PowerShell scripts by default in Windows to prevent an attacker from tricking you into
downloading and running malware.  You disable this security "feature" at your own risk.

To enable Powershell for your operating system (and enable scripts from explorer.exe)

    Set-ExecutionPolicy Unrestricted -Scope CurrentUser

Once you do this, you'll never have to think about PowerShell execution policy again.

If you prefer to open a PowerShell window that allows scripts in just that session, launch using (Win+R):

    powershell -ex unrestricted

OR in any current powershell window

    Set-ExecutionPolicy Unrestricted -Scope Process

## Known bug:

On some machines, there appears to be a race condition where the script doesn't load Windows Forms correctly.
Instead there are TypeNotFound errors that look like "Unable to find type [GroupBox]"  (If anybody out there knows 
how to fix this, I'm looking for help).  One workaround is to load Windows Forms before running the script.  Open
a powershell window and run:

    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing
    d:\path\to\pc20chapters.ps1


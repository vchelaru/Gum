# Opening Gum on macOS

Gum is not notarized by Apple, so macOS blocks it the first time you open it. You approve it once, and after that Gum opens normally. A new release is a new download, so you approve each new copy once too.

## Download and Extract

1. Download the file for your Mac from the [latest release](https://github.com/vchelaru/Gum/releases/latest):
   * `Gum-osx-arm64.tar.xz` for Apple Silicon (M1 and later).
   * `Gum-osx-x64.tar.xz` for Intel.

   To check which one you have, open the Apple menu and select **About This Mac**. A **Chip** line starting with **Apple** means Apple Silicon. A **Processor** line mentioning **Intel** means Intel.
2. Double-click the `.tar.xz` file in Finder. macOS extracts `Gum.app` into the same folder.
3. Drag `Gum.app` into your **Applications** folder.

## Allow Gum to Open

Use either the System Settings steps or the Terminal command. Both do the same job.

### Option 1: System Settings

1. Double-click `Gum.app`. macOS shows a message saying it could not verify Gum. Click **Done**, not **Move to Trash**.
2. Open the Apple menu and select **System Settings**, then **Privacy & Security**.
3. Scroll down to the **Security** section. It shows a message that Gum was blocked. Click **Open Anyway**.
4. Enter your password or use Touch ID, then click **Open Anyway** in the dialog that appears.

The **Open Anyway** button only appears for about an hour after macOS blocks Gum. If you don't see it, double-click `Gum.app` again and then go back to **Privacy & Security**.

On macOS 14 (Sonoma) and earlier there is a shortcut: Control-click `Gum.app` in Finder, select **Open**, then click **Open** in the dialog. macOS 15 (Sequoia) removed this shortcut, so use the steps above there.

### Option 2: Terminal

Open **Terminal** (in **Applications** > **Utilities**) and run:

```sh
xattr -dr com.apple.quarantine /Applications/Gum.app
```

macOS marks every downloaded file as coming from the internet, and that mark is what makes it check the app. This command removes the mark from `Gum.app` and everything inside it. If you put `Gum.app` somewhere other than **Applications**, change the path to match.

## Folder Access Prompts

macOS asks before any app reads files in your **Desktop**, **Documents**, or **Downloads** folders, or on an external drive. The first time Gum opens a project in one of those places, you may see a message like "Gum would like to access files in your Documents folder." Click **Allow**.

If you clicked **Don't Allow**, Gum cannot read the project. To fix it:

1. Open **System Settings** > **Privacy & Security** > **Files and Folders**.
2. Find **Gum** in the list and turn on the folder your project is in.
3. Quit and reopen Gum.

## Troubleshooting

* **macOS says Gum "is damaged and can't be opened."** Run the Terminal command in [Option 2](#option-2-terminal), then open Gum again.
* **Gum is blocked again after updating.** Each new download carries a new internet mark. Approve the new copy with either option above.
* **Gum opens but can't find your project.** Check the folder access steps in [Folder Access Prompts](#folder-access-prompts).

For other setup details, including how to open a project from the command line, see [Setup](README.md).

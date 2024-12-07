<link-summary>How to install Anchor Chain</link-summary>
<show-structure for="chapter,procedure" depth="2"/>

# Installing Anchor Chain

<note>
Installing Anchor Chain is made to be as simple as possible, but it still requires an understanding of how to use your computer.
If you do not know how to navigate your file system or download, unzip, and move files and folders, please ask someone in your life who is computer-literate to help you out.
</note>

## Installing BepInEx

Anchor Chain is based upon the free and open source Unity mod loader, 
<a href="https://docs.bepinex.dev/articles/index.html" summary="Official BepInEx documentation">BepInEx</a>.
Before we can install Anchor Chain, we first have to create a functioning BepInEx Install.

<tip>
If you prefer to follow the official BepInEx installation guide, it is available here: <a href="https://docs.bepinex.dev/articles/user_guide/installation/index.html"/>.
Make sure to install BepInEx version `5.4.x.x`.
</tip>

<procedure title="How To Install BepInEx For Sea Power" id="BepInEx">
<step>
Download <code>BepInEx_win_x64_5.4.x.x.zip</code> from the <a href="https://github.com/BepInEx/BepInEx">BepInEx github page</a>.
You can find the file under the "Releases" tab in the right hand sidebar.

If the version displayed there is not of the form `5.4.x.x`, then click the "+x releases" link and scroll to the most recent version satisfying that format.
</step>
<step>
Unzip BepInEx into a folder within your Downloads folder.
</step>
<step>
Repeatedly enter the unzipped <code>BepInEx_win_x64_5.4.x.x</code> folder until you see the <code>winhttp.dll</code> file.
</step>
<step>
Highlight and use <shortcut key="$Copy"/> or <shortcut key="$Cut"/> to copy or cut the BepInEx installation.
</step>
<step>
Naviagte to your Sea Power installation. 
It should be located under <code>C:\Program Files (x86)\Steam\steamapps\common\Sea Power\</code>.
You should now be in a folder that contains the <code>Sea Power.exe</code> file.
</step>
<step>
Press <shortcut key="$Paste"/> to paste BepInEx into your Sea Power installation. 
The folder you are in should now contain the BepInEx folder and the <code>winhttp.dll</code> file.
</step>
<step>
Double click the <code>Sea Power.exe</code> file to have BepInEx set itself up.
Don't worry if the game closes and re-opens a few times, this is normal.
</step>
<step>
To make sure that BepInEx initialized correctly, enter the <code>BepInEx</code> folder and look for the <code>LogOutput.log</code> file.
If it does not exist, then something went wrong. You'll need to troubleshoot on your own from here.
</step>

Congratulations, you have now installed BepInEx! Only two more steps to go before you can install your mods.
</procedure>

## Installing the Anchor Chain Preloader

<procedure title="How To Install the Anchor Chain Preloader">
<step>
Go to the <a href="https://github.com/SeaPower-Modders/AnchorChain">Anchor Chain github page</a> and download the <code>AnchorChain.Preloader.dll</code> file from the most recent release.
You can find the most recent release in the right hand sidebar underneath "Releases".
</step>
<step>
Go to your Downloads folder and use <shortcut key="$Copy"/> or <shortcut key="$Cut"/> to copy or cut the <code>AnchorChain.Preloader.dll</code> file.
</step>
<step>
Navigate to your Sea Power installation.
It should be located under <code>C:\Program Files (x86)\Steam\steamapps\common\Sea Power\</code>.
You should see the <code>BepInEx</code> folder that your created in <a href="Install-Anchor-Chain.md#BepInEx">the previous step</a>.
</step>
<step>
Open the <code>BepInEx</code> folder, then create or open the <code>plugins</code> folder within it.
</step>
<step>
Press <shortcut key="$Paste"/> to paste <code>AnchorChain.Preloader.dll</code> into the plugins folder.
</step>

You have now installed the Anchor Chain Preloader! 
This is what allows us to distribute the bulk of Anchor Chain via the Steam Workshop.
</procedure>

## Installing Anchor Chain

<procedure title="How To Install Anchor Chain">
<step>
Open Steam and navigate to the workshop by hovering over the "Community" tab in the top bar.
</step>
<step>
Type "Sea Power" into the search bar in the top right. Then click the "Sea Power" option in the dropdown.
</step>
<step>
Search "Anchor Chain" using the search bar in the top right, and click on the entry named "Anchor Chain".
If it does not appear, then fiddle with the "Sort By" setting above the results.
</step>
<step>
Click the green "Subscribe" button to have the Steam Workshop install Anchor Chain for you.
</step>
</procedure>

## After Installation

There is nothing else to do now other than play!
You are now ready to start installing mods using Anchor Chain and the Steam Workshop.
If you would like to learn how to install mods, then see our guide 
<a href="Installing-Mods.md">here</a>.
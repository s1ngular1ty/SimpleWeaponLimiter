<h2>Description</h2>
<p>This plugin was originally created to enforce weapon limits in a knife and pistol only server. I quickly realized it could be used to limit any weapon on any type of server.
    <br />
    <br />Unfortunately, because of the way Battlefield 4 reports vehicle kill information, vehicles can not be limited using this plugin.</p>
<p>This plugin limits players from using certain weapons, or categories of weapons, in game. Action is taken against players when they kill with a weapon or weapon category you have have restricted with this plugin. An escalating punishment system is built into the plugin. The punishments grow more severe until the player is temporarily banned for a time duration you can adjust. The punishment system is as follows:</p>
<p><strong>Punishment Pattern:</strong>
</p>
<ol>
    <li><strong>Warning 1</strong> - The offender is killed and warned that they used a restricted weapon via a admin yell directed to them and in game chat. The victim is also notified that the killer was punished to prevent victim rage.
        <br />
        <br />
    </li>
    <li><strong>Warning 2</strong> - The offender is warned again like they were warned in warning 1 and the victim is also notified.
        <br />
        <br />
    </li>
    <li><strong>Kick</strong> - The offender is kicked and notified in the kick message why they were kicked. Everyone in game is notified that the offender is kicked.
        <br />
        <br />
    </li>
    <li><strong>Temporary Ban</strong> - If the offender returns to the game and uses a restricted weapon again during the same game, they are temporarily banned from the server for a duration of your choosing. The ban duration can be set with the <strong>Temp Ban Duration</strong> setting in the plugin. The time you specify for the ban is in minutes.</li>
</ol>
<p>By default, this plugin restricts all weapons except those that you specifically allow. This makes it very easy to set up restrictions for a knife and pistol only server. The default configuration of the plugin is setup to allow only Melee weapons (knives), pistols, and the phantom bow (weapon code dlSHTR).</p>
<p>You can change this behavior if you wish with the <strong>Restriction Mode</strong> setting.&nbsp;</p>
<h2>&nbsp;</h2>
<h2>Settings</h2>
<ul>
    <li><strong>Enable White List? - </strong>This setting controls the player white list for the plugin. If set this setting is <strong>true</strong> then the player white list is activated. Any players in the player white list will be protected from punishments for using restricted weapons. If this setting is set to <strong>false</strong> then the white list is deactivated.
        <br />
        <br />
    </li>
    <li><strong>White List Admins? - </strong>This setting controls whether admins are immune to the weapon restrictions of the plugin. Setting this to <strong>true</strong> means admins are protected from punishments if they use a restricted weapon. Setting this to <strong>false</strong> means admins are restricted like all other players.
        <br />
        <br />
    </li>
    <li><strong>White List - </strong>This setting is a list of players you want to protect from being punished if they used a restricted weapon. Place one player name per line in the setting as shown below:
        <blockquote>S1ngular1ty
            <br /> Phogue
            <br /> RandonDude</blockquote>
    </li>
    <li><strong>Restriction Mode - </strong>This setting controls the how the plugin restricts weapons. There are two options. The first mode called <strong>WhiteList</strong> is the default behavior of the plugin.&nbsp; This mode restricts all weapons except those you explicitly allow. The second mode is called <strong>BlackList</strong> and it exhibits the opposite behavior. It allows all weapons except those that you explicitly restrict.
        <br />
        <br />
    </li>
    <li><strong>Allowed Weapon Categories - </strong>This setting is only available in the <strong>WhiteList</strong> restriction mode. Enter the weapon categories you wish to allow in this setting. You must enter one category per line as shown:
        <blockquote>Melee
            <br />Handgun
            <br />Impact</blockquote>
        <p>The allowable weapon categories are available in the BF4.def file in the \Configs folder of your Procon installation</p>
    </li>
    <li><strong>Disallowed Weapon Categories - </strong>This setting is only available in the <strong>BlackList</strong> restriction mode. Enter the weapon categories you wish to restrict in this setting. You must enter one category per line as shown:
        <blockquote>SniperRifle
            <br />ProjectileExplosive
            <br />Shotgun</blockquote>
        <p>The allowable weapon categories are available in the BF4.def file in the \Configs folder of your Procon installation</p>
    </li>
    <li><strong>Allowed Weapons - </strong>In the <strong>WhiteList</strong> restriction mode, this setting allows you to specify individual weapons that you wish to allow from weapon categories that are not allowed in the <strong>Allowed Weapon Categories</strong> setting or that don't have a weapon category. For example, if you want to allow the Phantom Bow you must place the weapon code dlSHTR in this setting because the Phantom Bow does not have a weapon category.
        <br />
        <br />In the <strong>BlackList</strong> restriction mode, this setting allows you to specify individual weapons that you wish to allow from weapon categories that you restricted in the <strong>Disallowed Weapon Categories</strong> setting. For example, if you wanted to limit sniper rifles to a few choices you could restrict the SniperRifle category and then allow the specific weapon codes for the rifles you want to allow by placing them in this setting.
        <br />
        <br />This setting requires you to enter 1 weapon per line as shown:
        <blockquote>dlSHTR
            <br />U_BallisticShield</blockquote>
        <p>The allowable weapon codes are available in the BF4.def file in the \Configs folder of your Procon installation</p>
    </li>
    <li><strong>Disallowed Weapons - </strong>In the <strong>WhiteList</strong> restriction mode, this setting allows you to specify individual weapons that you wish to restrict from weapon categories that are allowed in the <strong>Allowed Weapon Categories</strong> setting or that don't have a weapon category. A very common example of the usefulness of this setting would be to allow all handguns using the Handgun category in Allowed Weapon Categories while limiting the use of the G18 and M93 automatic pistols by by placing their weapon codes in this setting.
        <br />
        <br />In the <strong>BlackList</strong> restriction mode, this setting allows you to specify individual weapons that you wish to restrict from weapon categories that you have NOT restricted in the <strong>Disallowed Weapon Categories</strong> setting. For example, if you wish to restrict 1 sniper rifle out of all the sniper rifles you would enter the weapon code for that rifle in this setting.
        <br />
        <br />This setting requires you to enter 1 weapon per line as shown:
        <blockquote>U_Glock18
            <br />U_M93R
            <br />U_SerbuShorty</blockquote>
        <p>The allowable weapon categories are available in the BF4.def file in the \Configs folder of your Procon installation</p>
    </li>
    <li><strong>Temp Ban Duration (minutes) - </strong>This setting controls the temporary ban duration for players that repeatedly violate the restricted weapons rules during a game. The time show is in minutes.</li>
</ul>
<h2>&nbsp;</h2>
<h2>Weapon Categories</h2>
<p>These are the available weapon categories from the BF4.def file in the \Configs folder of your Procon installation.</p>
<blockquote>AssaultRifle
    <br />Carbine
    <br />DMR
    <br />Explosive
    <br />Handgun
    <br />Impact
    <br />LMG
    <br />Melee
    <br />Nonlethal
    <br />PDW
    <br />ProjectileExplosive
    <br />Shotgun
    <br />SMG
    <br />SniperRifle</blockquote>
<h2>&nbsp;</h2>
<h2>Commands</h2>
<blockquote>
    <h4>!checkaccount [playername]</h4>
    <ul>
        <li>Checks whether the player specified is an admin and sends message to you in chat</li>
    </ul>
</blockquote>
<blockquote>
    <h4>!decrpunish [playername]</h4>
    <ul>
        <li>Decreases the punishment level of a player by 1</li>
    </ul>
</blockquote>
<blockquote>
    <h4>!incrpunish [playername]</h4>
    <ul>
        <li>Increases the punishment level of a player by 1</li>
    </ul>
</blockquote>
<blockquote>
    <h4>!checkpunish [playername]</h4>
    <ul>
        <li>Checks the current punishment level of a player</li>
    </ul>
</blockquote>
<blockquote>
    <h4>!setpunish [playername] [value]</h4>
    <ul>
        <li>Sets the punishment level of a player to the value you specify</li>
        <li>Value can be between 0 and 2</li>
    </ul>
</blockquote>
<blockquote>
    <h4>!clearpunish [playername]</h4>
    <ul>
        <li>Clears the punishments for the player you specify</li>
    </ul>
</blockquote>
<h2>&nbsp;</h2>
<h2>Command Response Scopes</h2>
<blockquote>
    <h4>!</h4> Responses will be displayed to everyone in the server.</blockquote>
<blockquote>
    <h4>@</h4> Responses will only be displayed to the account holder that issued the command.</blockquote>
<p>All error messages are privately sent to the command issuer</p>
<h2>&nbsp;</h2>
<h2>Development</h2>
<h3>Changelog</h3>
<blockquote>
    <h4>1.0.0.0 (4-24-2016)</h4> - initial version</blockquote>

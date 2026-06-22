Features and changes, big and small, that have been proposed but not thoroughly scoped out for implementation.

* remove '\[b]The bot is currently under construction. If you are seeing this message, your profile will almost certainly be reset before release, so don't get too attached!\[/b]' warning text in !joinchateau now that the bot is mature and a reset is highly unlikely.
* Revisit all '\[b]This should not be taken lightly, and can not be done frequently\[/b]' text to better indicate actual cool down information (recipient or initiator, how frequent, etc).
* Revisit corruption text to indicate it will be visible in other interactions, and other implications
* A system to include multiple people in a casual interaction, and possibly other interactions.
* !cancel and !decline (initiator and recipient geared 'remove pending interaction' commands) on top of existing time out parameters.
* if someone is !employed by another (not themselves), give their employer some currency when the employee works. TOS prevents us from notifying the employer in response to an employee privately doing !work, so should also include some sort of command for employers to see how much employees have earned them
* additional casual commands. lapsit, boobhat, lick, possibly more. Will it take up too much dossier space? Do they have reasonable titles to accrue?
* !bondtree and/or !familytree which would map out all users connected to someone by N degrees of separation via bonds.
* Some way to resurface chips, poker, other games from the underlying dicebot functionality (currently, Chateau Work has overwritten the dicebot work). Save this for after original dice bot developer pushes their most recent dicebot changes to Git, for ease of merging
* Random events, to encourage spontaneous chat activity. Responding to a random event would always start !random but might also require an additional argument, to slow down campers/snipers
* Audit existing commands for sensible new aliases (and check for collisions before adding any). Surfaced while speccing !refuse/!withdraw, whose short aliases !r and !w need a collision check against the live command table.


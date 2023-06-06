whitelist-not-whitelisted = Nyanotrasen requires whitelisting above {$num} players. Connect to the Discord at www.nyanotrasen.moe
whitelist-end-round-kick = Non-whitelisted players are automatically kicked at round end. To apply for whitelisting, connect to the Discord at www.nyanotrasen.moe

command-whitelistadd-description = Adds the player with the given username to the server whitelist.
command-whitelistadd-help = whitelistadd <username>
command-whitelistadd-existing = {$username} is already on the whitelist!
command-whitelistadd-added = {$username} added to the whitelist
command-whitelistadd-not-found = Unable to find '{$username}'

command-whitelistremove-description = Removes the player with the given username from the server whitelist.
command-whitelistremove-help = whitelistremove <username>
command-whitelistremove-existing = {$username} is not on the whitelist!
command-whitelistremove-removed = {$username} removed from the whitelist
command-whitelistremove-not-found = Unable to find '{$username}'

# Nyanotrasen-Donator-start
command-donatoradd-description = Adds the player with the given username to the donator list with an optional rank and expiration time offset in days.
command-donatoradd-help = donatoradd <username> <rank> <expiration time>
command-donatoradd-existing = {$name} is already a donator!
command-donatoradd-invalid-time = Invalid time offset '{$time}', must be a positive integer in days.
command-donatoradd-added = {$name} added to the donator list.
command-donatoradd-not-found = Unable to find user with name '{$name}'

command-donatorremove-description = Removes the player with the given username from the donator list.
command-donatorremove-help = donatorremove <username>
command-donatorremove-existing = {$name} is not a donator!
command-donatorremove-removed = {$name} removed from the donator list.
command-donatorremove-not-found = Unable to find user with name '{$name}'

command-donatorget-description = Gets the donator status of the player with the given username.
command-donatorget-help = donatorget <username>
command-donatorget-not-found = Unable to find user with name '{$name}'
command-donatorget-donator = {$name} is a donator.
command-donatorget-not-donator = {$name} is not a donator.
# Nyanotrasen-Donator-end

command-kicknonwhitelisted-description = Kicks all non-whitelisted players from the server.
command-kicknonwhitelisted-help = kicknonwhitelisted

ban-banned-permanent = This ban will only be removed via appeal.
ban-banned-permanent-appeal = This ban will only be removed via appeal. You can appeal at {$link}
ban-expires = This ban is for {$duration} minutes and will expire at {$time} UTC.
ban-banned-1 = You, or another user of this computer or connection, are banned from playing here.
ban-banned-2 = The ban reason is: "{$reason}"
ban-banned-3 = Attempts to circumvent this ban such as creating a new account will be logged.

soft-player-cap-full = The server is full!
panic-bunker-account-denied = Due to Russian raiders recently, we are not accepting connections from new accounts right now.
                              If you speak good English and are really interested, join the Discord at www.nyanotrasen.moe
panic-bunker-no-admins = No admins are on, and your account is new to us.
                         To ensure game quality, we unfortunately have to reject this connection.
                         If you're interested in Nyanotrasen, please check out the website and Discord at www.nyanotrasen.moe

# About
This is a REST Api for Stoat (or any platform realistically) that generates a welcome image for your server

## How to setup
Start by craeting your mongo database with the deafult name of "serverInfo" (change in the config.json)
Add a collection called `authTokens` with a row called "token" and add your token (usually either your bot token or a JWST) 
The rest of the collections shuould create themselves

Then configure the json values (Main one needed to be changed is the `mongo.connectionString`)

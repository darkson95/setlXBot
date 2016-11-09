var Discord = require("discord.js");
var bot = new Discord.Client();
var stringArgv = require('string-argv');
var fs = require('fs');
var download = require('download');
var exec = require('child_process').exec;
var setlxPath = "./setlx/setlX";

bot.login("enter here your discord bot token");

bot.on('ready', () => {
	var game = new Discord.Game({name : "!help", type : 1});
	var pres = new Discord.Presence({status : "online", game : game});

	bot.user.setPresence(pres);

	console.log('I am ready!');
});

bot.on("message", msg => {
	var prefix = "!";
	var args = stringArgv.parseArgsStringToArgv(msg.content);
	var cmd = args[0] || "";
	args = args.filter(a => a !== cmd);

	if (msg.author.bot) return;
	if (!msg.content.startsWith(prefix)) return;

	if(msg.content.startsWith(prefix + "help")){
		msg.reply('\n\n!help - print all commands\n!setlx - executes stlx-file in message-attachment and prints result\n!setlx code - executes stlx-file in message-attachment and prints sourcecode and result\n');    
	}
	else if(cmd.startsWith(prefix + "setlx")){
		var att = msg.attachments.first();
		if(att){
			var fileName = att.filename;
			var fileUrl = att.url;
			var fileSaveDir = './tmp/';
			var fileSavePath = fileSaveDir + fileName;

			var fileExt = /(?:\.([^.]+))?$/.exec(fileName)[1];

			if(fileExt == "stlx"){
				download(fileUrl, './tmp/').then(() => {
				    var cmd = setlxPath + " " + fileSavePath;

    				if(args[0].startsWith("code")){
    					fs.readFile(fileSavePath, 'utf8', function (err, data) {
						  	if (err) throw err;

							msg.channel.sendCode('setlX', fileName + ' Code:\n\n' + data, {});
						});
					}

				    exec(cmd , (error, stdout, stderr) => {
					  	if (error) {
					    	msg.reply("Error (exec): " + error);
					    	return;
					  	}
					  	console.log(stdout);

					  	msg.channel.sendCode('', fileName + ' Result:\n\n' + stdout, {});

					  	setTimeout(function(){ 
					  		fs.unlink(fileSavePath, function(err){
			        			if (err) throw err;
				          	});
					  	}, 5000);
					});
				});
			}
			else {
				msg.reply("Error: attachment is not a .stlx file");
			}
		}
		else {
			msg.reply("Error: no attachment");
		}
	}
});

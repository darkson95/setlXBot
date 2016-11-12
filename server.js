var Discord = require("discord.js");
var bot = new Discord.Client();
var stringArgv = require('string-argv');
var fs = require('fs');
var download = require('download');
var exec = require('child_process').exec;
var setlxPath = "./setlx/setlX --runtimeDebugging ";

bot.login(process.env.BOTTOKEN);

bot.on('ready', () => {
	var game = new Discord.Game({name : "!info", type : 1});
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

	if(msg.content.startsWith(prefix + "commands")){
		msg.reply('\n\n!commands - print all commands\n!info - prints bot informations\n!setlx - executes stlx-file in message-attachment and prints result\n!setlx code - executes stlx-file in message-attachment and prints sourcecode and result\n!version - prints setlX version\n\n');
	}
	else if(cmd.startsWith(prefix + "info")){
		var ut = humanizeDuration(Math.round(bot.uptime / 1000)*1000);
		msg.reply('\n\nIf you found a bug or have a nice idea, please contact me or create an issue on GitHub!\n- Mail: setlxbot@wurstkun.com\n- Repository: https://github.com/darkson95/setlXBot\n- ``!commands`` - prints all commands\n- Bot-Uptime: ' + ut + '\n');
	}
	else if(cmd.startsWith(prefix + "setlx")){
		var att = msg.attachments.first();
		var func = args[0] || "";

		if(att){
			var fileName = att.filename;
			var fileUrl = att.url;
			var fileSaveDir = '/tmp/setlx/';
			var fileSavePath = fileSaveDir + fileName;
			var fileExt = /(?:\.([^.]+))?$/.exec(fileName)[1];

			if(fileExt == "stlx"){
				download(fileUrl, fileSaveDir).then(() => {

				    if(func.startsWith("code")){
    					fs.readFile(fileSavePath, 'utf8', function (err, data) {
						  	if (err) throw err;

							msg.channel.sendCode('setlX', fileName + ' Code:\n\n' + data, {});
						});
					}

				    msg.channel.sendMessage('Executing your setlX File...')
				    	.then(message => {
						    var command = setlxPath + fileSavePath;
				    		exec(command, (error, stdout, stderr) => {
							  	if (error) {
							    	msg.reply("Error (exec): " + error);
							    	return;
							  	}

							  	var output = fileName + ' Result:\n\n' + stdout;
							  	if(stderr.length > 3){
							  		output = output.concat('\n\nstderr:\n' + stderr);
							  	}

							  	msg.channel.sendCode('', output, {});
							  	message.delete();

							  	setTimeout(function(){ 
							  		fs.unlink(fileSavePath, function(err){
					        			if (err) throw err;
						          	});
							  	}, 3000);
							});
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
	else if(cmd.startsWith(prefix + "version")){
		var command = setlxPath + ' --version';
		exec(command, (error, stdout, stderr) => {
		  	if (error) {
		    	msg.reply("Error (exec): " + error);
		    	return;
		  	}

		  	msg.channel.sendCode('', 'setlX --version:\n' + stdout, {});
		});
	}
});

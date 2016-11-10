var Discord = require("discord.js");
var bot = new Discord.Client();
var stringArgv = require('string-argv');
var fs = require('fs');
var download = require('download');
var exec = require('child_process').exec;
var setlxPath = "./setlx/setlX --runtimeDebugging ";

bot.login(process.env.BOTTOKEN);

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
		msg.reply('\n\n!help - print all commands\n!setlx - executes stlx-file in message-attachment and prints result\n!setlx code - executes stlx-file in message-attachment and prints sourcecode and result\n\n');
	}
	else if(cmd.startsWith(prefix + "setlx")){
		var att = msg.attachments.first();
		var func = args[0] || "";

		if(att){
			var fileName = att.filename;
			var fileUrl = att.url;
			var fileSaveDir = '/tmp/setlx';
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
						    var cmd = setlxPath + fileSavePath;
				    		exec(cmd, {timeout : 60}, (error, stdout, stderr) => {
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
});

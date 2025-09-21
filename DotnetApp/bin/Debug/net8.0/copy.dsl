script(main)
{
	curdir = getscriptdir();
	cd(curdir);
	fileecho(true);
	
	copyfiles(".", "../../../../build/managed","DotnetApp.*");
	
	if (argnum() <= 1) {
		echo("press any key ...");
		read();
	};
	return(0);
};
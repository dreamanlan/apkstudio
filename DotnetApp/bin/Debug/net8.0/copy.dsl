script(main)
{
	curdir = getscriptdir();
	cd(curdir);
	fileecho(true);
	
	copyfiles(".", "../../../../build/managed","DotnetApp.*");
	copyfiles(".", "../../../../build/managed","Common.*");
	copyfiles(".", "../../../../build/managed","DotnetStoryScript.*");
	copyfiles(".", "../../../../build/managed","Dsl.*");
	copyfiles(".", "../../../../build/managed","LitJson.*");
	copyfiles(".", "../../../../build/managed","ScriptFrameworkLibrary.*");
	
	if (argnum() <= 1) {
		echo("press any key ...");
		read();
	};
	return(0);
};
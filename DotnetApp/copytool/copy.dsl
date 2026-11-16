script(main)
{
	curdir = get_script_directory();
	cd(curdir);
	file_echo(true);
	
	copy_files(".", "../../../../build/managed","DotnetApp.*");
	copy_files(".", "../../../../build/managed","Common.*");
	copy_files(".", "../../../../build/managed","DotnetStoryScript.*");
	copy_files(".", "../../../../build/managed","Dsl.*");
	copy_files(".", "../../../../build/managed","LitJson.*");
	copy_files(".", "../../../../build/managed","ScriptFrameworkLibrary.*");
	
	if (arg_num() <= 1) {
		echo("press any key ...");
		read();
	};
	return(0);
};
{ pkgs }: {
	deps = [
		pkgs.nodePackages.prettier
		pkgs.jq.bin
  pkgs.dotnet-sdk
    pkgs.omnisharp-roslyn
	];
}
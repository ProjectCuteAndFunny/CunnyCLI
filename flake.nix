{
  description = "A very basic flake";

  outputs = { self, nixpkgs }: {

    packages.x86_64-linux.cunnycli = { lib, stdenv, fetchFromGitHub, buildDotnetPackage, dotnetPackages, pkg-config }:
    buildDotnetPackage rec {
      pname = "CunnyCLI";
      baseName = pname; # workaround for "called without baseName"
      version = "1";
      src = ./.;
      projectFile = [ ./CunnyCLI.csproj ];
      nativeBuildInputs = [
        pkg-config
      ];
      meta = with lib; {
        homepage = "some_homepage";
        description = "some_description";
        license = licenses.mit;
      };
    }

    packages.x86_64-linux.default = self.packages.x86_64-linux.hello;

  };
}

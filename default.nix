{ lib
, stdenv
, fetchNuGet
, buildDotnetModule
, dotnet-sdk_6
, dotnet-runtime_6
, pkg-config }:

buildDotnetModule rec {
  pname = "CunnyCLI";
  version = "1";
  src = ./.;
  nugetDeps = ./deps.nix;
s  dotnet-sdk = dotnet-sdk_6;
  dotnet-runtime = dotnet-runtime_6;
  meta = with lib; {
    homepage = "https://github.com/ProjectCuteAndFunny/CunnyCLI";
    description = "A tool that uses the CunnyAPI to download images";
    license = licenses.gpl3;
    platforms = [ "x86_64-linux" ];
  };
}

{ lib
, stdenv
, fetchNuGet
, buildDotnetModule
, dotnet-sdk_7
, dotnet-runtime_7
, pkg-config }:

buildDotnetModule rec {
  pname = "CunnyCLI";
  version = "1";
  src = ./.;
  nugetDeps = ./deps.nix;
  dotnet-sdk = dotnet-sdk_7;
  dotnet-runtime = dotnet-runtime_7;
  meta = with lib; {
    homepage = "https://github.com/ProjectCuteAndFunny/CunnyCLI";
    description = "A tool that uses the CunnyAPI to download images";
    license = licenses.gpl3;
    platforms = [ "x86_64-linux" ];
  };
}

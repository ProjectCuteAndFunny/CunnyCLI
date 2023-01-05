{
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-22.11";
  };
  
  outputs = { self, nixpkgs }: let
    system = "x86_64-linux";
    pkgs = import nixpkgs {
      inherit system;
    };
  in {

    packages.${system} = rec {
      cunnycli = pkgs.callPackage ./default.nix {};

      default = cunnycli;
    };

    devShells.${system}.default = pkgs.mkShell {
      buildInputs = with pkgs; [
        dotnet-sdk_6
        nuget-to-nix
        mktemp
      ];
      shellHook = ''
        update-deps() {
          tmp=$(mktemp -d)
          dotnet restore --packages $tmp
          nuget-to-nix $tmp >deps.nix
          rm -r $tmp
          unset tmp
          dotnet restore
        }
      '';
    };
  };
}

{
  description = "Open Terraria API build environment";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs = { nixpkgs, ... }:
    let
      systems = [
        "aarch64-darwin"
        "aarch64-linux"
        "x86_64-darwin"
        "x86_64-linux"
      ];

      forAllSystems = f:
        nixpkgs.lib.genAttrs systems (system:
          let
            pkgs = import nixpkgs { inherit system; };
            dotnetSdk =
              if pkgs ? dotnet-sdk_9 then
                pkgs.dotnet-sdk_9
              else
                pkgs.dotnetCorePackages.sdk_9_0;

            mkApp = name: text:
              let
                program = pkgs.writeShellApplication {
                  inherit name text;
                  runtimeInputs = [
                    dotnetSdk
                  ];
                };
              in
              {
                type = "app";
                program = "${program}/bin/${name}";
              };
          in
          f { inherit pkgs dotnetSdk mkApp; });
    in
    {
      apps = forAllSystems ({ mkApp, ... }: {
        default = mkApp "build-otapi" ''
          set -euo pipefail

          if [ -n "''${OTAPI_ROOT:-}" ]; then
            repo="$OTAPI_ROOT"
          elif [ -f OTAPI.sln ]; then
            repo="$PWD"
          elif [ -f src/Libraries/Open-Terraria-API/OTAPI.sln ]; then
            repo="$PWD/src/Libraries/Open-Terraria-API"
          else
            echo "Could not locate the Open-Terraria-API checkout. Set OTAPI_ROOT." >&2
            exit 1
          fi

          configuration="''${CONFIGURATION:-Debug}"

          cd "$repo"

          if [ ! -f FNA/FNA.Core.csproj ]; then
            echo "FNA submodule is missing. Run: git submodule update --init --recursive" >&2
            exit 1
          fi

          dotnet restore OTAPI.Mods.slnf
          dotnet build OTAPI.Mods.slnf --no-restore -c "$configuration"
        '';

        build = mkApp "build-otapi" ''
          set -euo pipefail

          if [ -n "''${OTAPI_ROOT:-}" ]; then
            repo="$OTAPI_ROOT"
          elif [ -f OTAPI.sln ]; then
            repo="$PWD"
          elif [ -f src/Libraries/Open-Terraria-API/OTAPI.sln ]; then
            repo="$PWD/src/Libraries/Open-Terraria-API"
          else
            echo "Could not locate the Open-Terraria-API checkout. Set OTAPI_ROOT." >&2
            exit 1
          fi

          configuration="''${CONFIGURATION:-Debug}"

          cd "$repo"

          if [ ! -f FNA/FNA.Core.csproj ]; then
            echo "FNA submodule is missing. Run: git submodule update --init --recursive" >&2
            exit 1
          fi

          dotnet restore OTAPI.Mods.slnf
          dotnet build OTAPI.Mods.slnf --no-restore -c "$configuration"
        '';

        tshock = mkApp "build-otapi-tshock" ''
          set -euo pipefail

          if [ -n "''${OTAPI_ROOT:-}" ]; then
            repo="$OTAPI_ROOT"
          elif [ -f OTAPI.sln ]; then
            repo="$PWD"
          elif [ -f src/Libraries/Open-Terraria-API/OTAPI.sln ]; then
            repo="$PWD/src/Libraries/Open-Terraria-API"
          else
            echo "Could not locate the Open-Terraria-API checkout. Set OTAPI_ROOT." >&2
            exit 1
          fi

          configuration="''${CONFIGURATION:-Debug}"
          framework="''${FRAMEWORK:-net9.0}"

          cd "$repo"

          if [ ! -f FNA/FNA.Core.csproj ]; then
            echo "FNA submodule is missing. Run: git submodule update --init --recursive" >&2
            exit 1
          fi

          dotnet restore OTAPI.Mods.slnf
          dotnet restore OTAPI.Server.Launcher.slnf
          dotnet build OTAPI.Mods.slnf --no-restore -c "$configuration"

          # The launcher project enables TML when generated tModLoader files exist.
          # This app is for the TShock/DGSP PC-server path, so discard those outputs.
          rm -rf \
            "OTAPI.Patcher/bin/$configuration/$framework/tModLoader" \
            "OTAPI.Server.Launcher/bin/$configuration/$framework/tModLoader" \
            "OTAPI.Server.Launcher/bin/$configuration/$framework/FNA.dll" \
            "OTAPI.Server.Launcher/bin/$configuration/$framework/FNA.pdb"

          run_patch_target() {
            local target="$1"
            (
              cd "OTAPI.Patcher/bin/$configuration/$framework"
              dotnet run \
                --project ../../../OTAPI.Patcher.csproj \
                --configuration "$configuration" \
                --framework "$framework" \
                -- \
                "-patchTarget=$target" \
                -latest=n
            )
          }

          test_server_launcher() {
            dotnet build OTAPI.Server.Launcher.slnf --no-restore -c "$configuration"
            (
              cd "OTAPI.Server.Launcher/bin/$configuration/$framework"
              dotnet OTAPI.Server.Launcher.dll -test-init
            )
          }

          run_patch_target p
          test_server_launcher
        '';
      });

      devShells = forAllSystems ({ pkgs, dotnetSdk, ... }: {
        default = pkgs.mkShell {
          packages = [
            dotnetSdk
          ];
        };
      });
    };
}

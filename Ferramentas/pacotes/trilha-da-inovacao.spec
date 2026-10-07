# Binários do Unity: sem strip, sem pacote de debug e sem dependências automáticas.
%global debug_package %{nil}
%global __os_install_post %{nil}
%global _build_id_links none
%global __requires_exclude_from ^/opt/trilha-da-inovacao/.*$
%global __provides_exclude_from ^/opt/trilha-da-inovacao/.*$

Name:           trilha-da-inovacao
Version:        %{versao}
Release:        1
Summary:        Trilha da Inovação: marcos tecnológicos do Maranhão (1986-2025) em VR
License:        Proprietary; modelos 3D Kenney CC0
URL:            https://kenney.nl
BuildArch:      x86_64
AutoReqProv:    no
Requires:       hicolor-icon-theme

%description
Experiência em realidade virtual da Fofucho's Games Group (Tecnologias Emergentes)
que percorre seis marcos tecnológicos do Maranhão entre 1986 e 2025. Roda com
headset OpenXR (Quest via Link/ALVR, SteamVR) ou em modo desktop com teclado e mouse.

%install
mkdir -p %{buildroot}/opt/%{name} %{buildroot}%{_bindir}
cp -a %{_sourcedir}/Linux/. %{buildroot}/opt/%{name}/
chmod 755 %{buildroot}/opt/%{name}/TrilhaDaInovacao.x86_64
cat > %{buildroot}%{_bindir}/%{name} <<'SH'
#!/bin/sh
exec /opt/trilha-da-inovacao/TrilhaDaInovacao.x86_64 "$@"
SH
chmod 755 %{buildroot}%{_bindir}/%{name}
install -Dm644 %{_sourcedir}/icone.png %{buildroot}%{_datadir}/icons/hicolor/512x512/apps/%{name}.png
install -Dm644 %{_sourcedir}/%{name}.desktop %{buildroot}%{_datadir}/applications/%{name}.desktop

%files
/opt/%{name}
%{_bindir}/%{name}
%{_datadir}/applications/%{name}.desktop
%{_datadir}/icons/hicolor/512x512/apps/%{name}.png

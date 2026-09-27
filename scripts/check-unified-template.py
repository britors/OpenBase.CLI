#!/usr/bin/env python3
"""Exercise the built CLI and the real template package in isolated SDK/user state."""
import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser()
parser.add_argument('--package', required=True, type=Path)
parser.add_argument('--template-root', required=True, type=Path)
parser.add_argument('--cli', type=Path, default=Path('bin/Release/net10.0/OpenBase.CLI.dll'))
args = parser.parse_args()
cli = args.cli.resolve()
package = args.package.resolve()
contracts = args.template_root.resolve() / 'contracts'

with tempfile.TemporaryDirectory(prefix='openbase-cli-e2e-') as temp:
    root = Path(temp)
    env = dict(os.environ, DOTNET_CLI_HOME=str(root / 'dotnet'), APPDATA=str(root / 'appdata'),
               OPENBASE_STATE_HOME=str(root / 'state'), DOTNET_NOLOGO='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',
               DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_CLI_UI_LANGUAGE='en-US')
    for p in ('dotnet', 'appdata', 'state'):
        (root / p).mkdir()

    def run(argv, expected=0, cwd=root):
        result = subprocess.run(argv, cwd=cwd, env=env, text=True, capture_output=True, timeout=300)
        assert result.returncode == expected, (argv[:3], expected, result.returncode, result.stdout, result.stderr)
        return result.stdout

    def invoke(argv, expected=0, cwd=root):
        text = run(['dotnet', str(cli), *argv, '--json'], expected, cwd)
        value = json.loads(text)
        assert value['protocolVersion'] == 1 and value['ok'] == (expected == 0), text
        assert value['error'] is None if expected == 0 else value['data'] is None
        return value

    missing = invoke(['new', '-n', 'Missing', '-d', 'postgres'], 3)
    assert missing['error']['code'] == 'TEMPLATE_NOT_INSTALLED'
    assert not (root / 'Missing').exists()
    installed = invoke(['install', '--package', str(package)])
    assert installed['data']['packageId'] == 'w3ti.OpenBaseNET.Template'
    assert installed['data']['version'] == '11.0.0-preview.1'

    if os.name != 'nt':
        import pty
        import select
        import signal
        import time

        def terminal_case(cancel):
            master, slave = pty.openpty()
            destination = root / ('terminal-cancel' if cancel else 'terminal-create')
            child = subprocess.Popen(['dotnet', str(cli), 'new', '-n', 'TerminalApi', '-o', str(destination)],
                                     stdin=slave, stdout=slave, stderr=slave, cwd=root, env=dict(env, TERM='xterm'),
                                     start_new_session=True)
            os.close(slave)
            captured = b''
            try:
                until = time.monotonic() + 30
                while b'Banco de dados' not in captured and time.monotonic() < until:
                    if select.select([master], [], [], 0.1)[0]:
                        captured += os.read(master, 65536)
                assert b'Banco de dados' in captured, captured
                if cancel:
                    child.send_signal(signal.SIGINT)
                else:
                    os.write(master, b'\r')
                until = time.monotonic() + 60
                while child.poll() is None and time.monotonic() < until:
                    if select.select([master], [], [], 0.1)[0]:
                        try:
                            captured += os.read(master, 65536)
                        except OSError:
                            break
                assert child.wait(timeout=5) == (130 if cancel else 0), captured
                assert destination.exists() != cancel
                if not cancel:
                    assert json.loads((destination / '.openbase.json').read_text())['database'] == 'postgres'
            finally:
                if child.poll() is None:
                    child.kill()
                    child.wait()
                os.close(master)

        terminal_case(False)
        terminal_case(True)
        print('Interactive selection and Ctrl+C cancellation passed', flush=True)

    def normalized(path):
        text = path.read_text(encoding='utf-8-sig')
        return re.sub(r'[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}', 'GUID', text)

    for database in ('postgres', 'sqlserver', 'oracle'):
        destination = root / f'{database} CLI with spaces'
        direct = root / f'{database} dotnet with spaces'
        result = invoke(['new', '-n', 'Acme.Customers', '-d', database, '-o', str(destination), '--non-interactive'])
        assert result['data']['projectRoot'] == str(destination)
        run(['dotnet', 'new', 'openbasenet', '--name', 'Acme.Customers', '--database', database, '--output', str(direct)])
        files = {p.relative_to(destination) for p in destination.rglob('*') if p.is_file()}
        assert files == {p.relative_to(direct) for p in direct.rglob('*') if p.is_file()}
        for relative in files:
            assert normalized(destination / relative) == normalized(direct / relative), relative
        manifest = json.loads((destination / '.openbase.json').read_text())
        assert manifest['database'] == database and manifest['schemaVersion'] == 2
        run(['dotnet', 'build', manifest['solution'], '-c', 'Release'], cwd=destination)
        run(['dotnet', 'test', 'tests/Acme.Customers.Tests.Unit', '-c', 'Release', '--no-build'], cwd=destination)
        occupied = invoke(['new', '-n', 'Acme.Customers', '-d', database, '-o', str(destination)], 2)
        assert occupied['error']['code'] == 'DESTINATION_NOT_EMPTY'
        guarded = invoke(['scaffold', '--entity', 'Probe'], 3, cwd=destination)
        assert guarded['error']['code'] == 'CAPABILITY_UNAVAILABLE'
        print(f'{database}: CLI/direct files match, build and unit tests passed', flush=True)

    cases = json.loads((contracts / 'creation-cases.json').read_text())['cases']
    for case in cases:
        expected = case['expected']
        if expected.get('prompt'):
            continue  # interactive terminal covered below on POSIX
        target = root / case['name']
        result = invoke([*case['arguments'], '-o', str(target)], expected['exitCode'])
        if expected['exitCode'] == 0:
            assert json.loads((target / '.openbase.json').read_text())['database'] == expected['database']
            if 'warningCode' in expected:
                assert any(w['code'] == expected['warningCode'] for w in result['warnings'])
        else:
            assert result['error']['code'] == expected['errorCode']
            assert not target.exists()
    for invalid in ('class', '1Api', 'Acme.class', 'My-Api', '@namespace', 'Api\n'):
        assert invoke(['new', '-n', invalid, '-d', 'postgres'], 2)['error']['code'] == 'NAME_INVALID'
    assert invoke(['new', '-n', 'Test', '-d'], 2)['error']['code'] == 'ARGUMENT_INVALID'
    assert invoke(['new', '-n', 'Test', '--unknown', 'credential-do-not-echo'], 2)['error']['code'] == 'ARGUMENT_INVALID'

    target = root / 'secrets'
    secret = 'e2e-only;password"value'
    result = invoke(['new', '-n', 'SecretApi', '-d', 'postgres', '-o', str(target),
                     '--db-user', 'test', '--db-password', secret, '--mediatr-license', 'ignored-license'])
    assert secret not in json.dumps(result) and 'ignored-license' not in json.dumps(result)
    m = json.loads((target / '.openbase.json').read_text())
    api = target / m['projects']['api']
    assert json.loads((api.parent / 'appsettings.json').read_text())['ConnectionStrings']['Default'] == ''
    sid = ET.parse(api).find('.//UserSecretsId').text
    stored = json.loads((root / 'appdata' / 'Microsoft' / 'UserSecrets' / sid / 'secrets.json').read_text())
    assert 'ConnectionStrings:Default' in stored and 'e2e-only' in stored['ConnectionStrings:Default']
    for p in target.rglob('*'):
        if p.is_file() and p.suffix in ('.json', '.cs', '.csproj'):
            assert 'e2e-only' not in p.read_text(encoding='utf-8-sig')
    assert invoke(['version', 'show'])['data']['packages']['w3ti.OpenBaseNET.Template'] == '11.0.0-preview.1'
    history = invoke(['history', '--type', 'template'])['data']['entries']
    assert len(history) == 1 and history[0]['success']
    assert not invoke(['history', '--type', 'oracle'])['data']['entries']
    print('Shared argument cases, errors, secrets, version/history and legacy guard passed', flush=True)

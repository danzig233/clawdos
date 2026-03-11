"""
clawdos_skill/__init__.py
OpenClaw Skill entry point — registers all Clawdos tools.
"""

from openclaw import Skill

from .config import ClawdosConfig
from .client import ClawdosClient
from .tools import (
    health_check, get_env,
    screen_capture,
    mouse_click, mouse_move, mouse_drag, mouse_scroll,
    key_combo, type_text, input_batch,
    window_list, window_focus,
    fs_list, fs_read, fs_write,
    fs_mkdir, fs_delete, fs_move,
    shell_exec,
)


def create_skill(config: dict | None = None) -> Skill:
    """
    Factory called by the OpenClaw runtime.

    Parameters
    ----------
    config : dict
        Skill-level config from the OpenClaw manifest / environment,
        must include 'api_key'; optionally 'base_url', 'timeout', 'fs_root_id'.

    Returns
    -------
    Skill
        A fully configured OpenClaw Skill with 18 tools.
    """
    cfg = ClawdosConfig.from_skill_config(config or {})
    client = ClawdosClient(cfg)

    skill = Skill(
        name="clawdos",
        description="Windows Execution Interface for OpenClaw skill — screen capture, input, window, filesystem, shell",
    )

    all_tools = [
        health_check, get_env,
        screen_capture,
        mouse_click, mouse_move, mouse_drag, mouse_scroll,
        key_combo, type_text, input_batch,
        window_list, window_focus,
        fs_list, fs_read, fs_write,
        fs_mkdir, fs_delete, fs_move,
        shell_exec,
    ]

    for t in all_tools:
        skill.register(t, client=client)

    return skill
"""
clawdos_skill/config.py
Clawdos Skill configuration.
"""

import os
from dataclasses import dataclass, field


@dataclass
class ClawdosConfig:
    """Runtime configuration for the Clawdos skill."""

    base_url: str = field(
        default_factory=lambda: os.getenv("CLAWDOS_BASE_URL", "http://127.0.0.1:17171")
    )
    api_key: str = field(
        default_factory=lambda: os.getenv("CLAWDOS_API_KEY", "")
    )
    timeout: int = field(
        default_factory=lambda: int(os.getenv("CLAWDOS_TIMEOUT", "30"))
    )
    fs_root_id: int = field(
        default_factory=lambda: int(os.getenv("CLAWDOS_FS_ROOT_ID", "0"))
    )

    def headers(self) -> dict:
        return {"X-Api-Key": self.api_key}

    @classmethod
    def from_skill_config(cls, config: dict) -> "ClawdosConfig":
        """Create from OpenClaw skill config dict."""
        return cls(
            base_url=config.get("base_url", cls.base_url),
            api_key=config.get("api_key", ""),
            timeout=config.get("timeout", 30),
            fs_root_id=config.get("fs_root_id", 0),
        )
#!/usr/bin/env python3

#
# Sends updates to a Discord webhook for new changelog entries since the last GitHub Actions publish run.
# Automatically figures out the last run and changelog contents with the GitHub API.
#

import io
import itertools
import os
import requests
import yaml
from dateutil import parser
from typing import Any, Iterable

GITHUB_API_URL    = os.environ.get("GITHUB_API_URL", "https://api.github.com")
GITHUB_REPOSITORY = os.environ["GITHUB_REPOSITORY"]
GITHUB_RUN        = os.environ["GITHUB_RUN_ID"]
GITHUB_TOKEN      = os.environ["GITHUB_TOKEN"]

DISCORD_WEBHOOK_URL = os.environ.get("DISCORD_WEBHOOK_URL")

CHANGELOG_FILE = "Resources/Changelog/DeltaVChangelog.yml"
CHANGELOG_FILE_UPSTREAM = "Resources/Changelog/Changelog.yml"

TYPES_TO_EMOJI = {
    "Fix":    "🐛",
    "Add":    "🆕",
    "Remove": "❌",
    "Tweak":  "⚒️"
}

ChangelogEntry = dict[str, Any]

def main():
    if not DISCORD_WEBHOOK_URL:
        return

    session = requests.Session()
    session.headers["Authorization"]        = f"Bearer {GITHUB_TOKEN}"
    session.headers["Accept"]               = "Accept: application/vnd.github+json"
    session.headers["X-GitHub-Api-Version"] = "2022-11-28"

    most_recent = get_most_recent_workflow(session)
    last_sha = most_recent['head_commit']['id']
    print(f"Last successsful publish job was {most_recent['id']}: {last_sha}")
    last_changelog = get_last_changelog(session, last_sha)
    with open(CHANGELOG_FILE, "r") as f:
        cur_changelog_tmp1 = yaml.safe_load(f)
    with open(CHANGELOG_FILE_UPSTREAM, "r") as f:
        cur_changelog_tmp2 = yaml.safe_load(f)
    cur_changelog = merge_changelog(cur_changelog_tmp1, cur_changelog_tmp2)

    diff = diff_changelog(last_changelog, cur_changelog)
    send_to_discord(diff)


def get_most_recent_workflow(sess: requests.Session) -> Any:
    workflow_run = get_current_run(sess)
    past_runs = get_past_runs(sess, workflow_run)
    for run in past_runs['workflow_runs']:
        # First past successful run that isn't our current run.
        if run["id"] == workflow_run["id"]:
            continue

        return run


def get_current_run(sess: requests.Session) -> Any:
    resp = sess.get(f"{GITHUB_API_URL}/repos/{GITHUB_REPOSITORY}/actions/runs/{GITHUB_RUN}")
    resp.raise_for_status()
    return resp.json()


def get_past_runs(sess: requests.Session, current_run: Any) -> Any:
    """
    Get all successful workflow runs before our current one.
    """
    params = {
        "status": "success",
        "created": f"<={current_run['created_at']}"
    }
    resp = sess.get(f"{current_run['workflow_url']}/runs", params=params)
    resp.raise_for_status()
    return resp.json()


def get_last_changelog(sess: requests.Session, sha: str) -> str:
    """
    Use GitHub API to get the previous version of the changelog YAML (Actions builds are fetched with a shallow clone)
    """
    params = {
        "ref": sha,
    }
    headers = {
        "Accept": "application/vnd.github.raw"
    }

    resp = sess.get(f"{GITHUB_API_URL}/repos/{GITHUB_REPOSITORY}/contents/{CHANGELOG_FILE}", headers=headers, params=params)
    resp.raise_for_status()
    master = yaml.safe_load(resp.text)
    resp2 = sess.get(f"{GITHUB_API_URL}/repos/{GITHUB_REPOSITORY}/contents/{CHANGELOG_FILE_UPSTREAM}", headers=headers, params=params)
    resp2.raise_for_status()
    upstream = yaml.safe_load(resp2.text)

    merged = merge_changelog(master,upstream)
    return merged

def merge_changelog(main: dict[str, Any], upstream: dict[str, Any]) -> Iterable[ChangelogEntry]:
    """
    Merge 2 separate changelog files with possible matching IDs and reset the combined IDs by date
    """
    combined = {key:[*main[key], *upstream[key]] for key in main}
    combined["Entries"].sort(key=lambda x: parser.parse(x["time"]))
    for count, entry in enumerate(combined["Entries"], start=1):
        entry["id"] = count
    return combined

def diff_changelog(old: dict[str, Any], cur: dict[str, Any]) -> Iterable[ChangelogEntry]:
    """
    Find all new entries not present in the previous publish.
    """
    old_entry_ids = {e["id"] for e in old["Entries"]}
    return (e for e in cur["Entries"] if e["id"] not in old_entry_ids)


def send_to_discord(entries: Iterable[ChangelogEntry]) -> None:
    if not DISCORD_WEBHOOK_URL:
        return

    content = io.StringIO()
    for name, group in itertools.groupby(entries, lambda x: x["author"]):
        content.write(f"**{name}** updated:\n")
        for entry in group:
            for change in entry["changes"]:
                emoji = TYPES_TO_EMOJI.get(change['type'], "❓")
                message = change['message']
                content.write(f"{emoji} {message}\n")

    body = {
        "content": content.getvalue(),
        # Do not allow any mentions.
        "allowed_mentions": {
            "parse": []
        },
        # SUPPRESS_EMBEDS
        "flags": 1 << 2
    }

    requests.post(DISCORD_WEBHOOK_URL, json=body)


main()

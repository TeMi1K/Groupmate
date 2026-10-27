import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api, errorMessage } from "../api";
import { ErrorBox } from "../components";
import { useToast } from "../context";
import { todayIso } from "../format";
import type { Project, Skill } from "../types";

/** Projekta izveide un publicēšana (prasība 4.1., projektējums 5.1.3). */
export default function NewProjectPage() {
  const navigate = useNavigate();
  const toast = useToast();
  const [catalog, setCatalog] = useState<Skill[]>([]);
  const [roles, setRoles] = useState<string[]>([]);

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [teamSize, setTeamSize] = useState(4);
  const [deadline, setDeadline] = useState(todayIso(14));
  const [endDate, setEndDate] = useState(todayIso(60));
  const [skills, setSkills] = useState<{ skillId: number; minLevel: number }[]>([]);
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    Promise.all([api<Skill[]>("/skills"), api<string[]>("/skills/roles")])
      .then(([c, r]) => {
        setCatalog(c);
        setRoles(r);
      })
      .catch((e) => setError(errorMessage(e)));
  }, []);

  const used = new Set(skills.map((s) => s.skillId));
  const freeSkill = catalog.find((c) => !used.has(c.id));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      const created = await api<Project>("/projects", {
        method: "POST",
        body: {
          title,
          description,
          teamSize,
          applicationDeadline: deadline,
          endDate,
          requiredSkills: skills,
          requiredRoles: selectedRoles,
        },
      });
      toast("Projekts publicēts.");
      navigate(`/org/projects/${created.id}`);
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <div className="page-head">
        <h1>Jauns projekts</h1>
      </div>
      <form className="card narrow-wide" onSubmit={submit}>
        <label>
          Nosaukums
          <input value={title} onChange={(e) => setTitle(e.target.value)} />
        </label>
        <label>
          Apraksts
          <textarea rows={4} value={description} onChange={(e) => setDescription(e.target.value)} />
        </label>
        <div className="row">
          <label>
            Komandas lielums (2–10)
            <input type="number" value={teamSize} onChange={(e) => setTeamSize(Number(e.target.value))} />
          </label>
          <label>
            Pieteikšanās termiņš
            <input type="date" value={deadline} onChange={(e) => setDeadline(e.target.value)} />
          </label>
          <label>
            Beigu datums
            <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
          </label>
        </div>

        <h3>Nepieciešamās prasmes un minimālais līmenis</h3>
        {skills.map((row, i) => (
          <div className="row row-tight" key={i}>
            <select
              aria-label="Prasme"
              value={row.skillId}
              onChange={(e) => setSkills(skills.map((s, j) => (j === i ? { ...s, skillId: Number(e.target.value) } : s)))}
            >
              {catalog
                .filter((c) => c.id === row.skillId || !used.has(c.id))
                .map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
            </select>
            <select
              aria-label="Minimālais līmenis"
              className="select-small"
              value={row.minLevel}
              onChange={(e) => setSkills(skills.map((s, j) => (j === i ? { ...s, minLevel: Number(e.target.value) } : s)))}
            >
              {[1, 2, 3, 4, 5].map((l) => (
                <option key={l} value={l}>
                  ≥ {l}
                </option>
              ))}
            </select>
            <button
              type="button"
              className="btn btn-small btn-ghost"
              aria-label="Noņemt"
              onClick={() => setSkills(skills.filter((_, j) => j !== i))}
            >
              ✕
            </button>
          </div>
        ))}
        <button
          type="button"
          className="btn btn-small"
          disabled={!freeSkill}
          onClick={() => freeSkill && setSkills([...skills, { skillId: freeSkill.id, minLevel: 3 }])}
        >
          + Pievienot prasmi
        </button>

        <h3>Nepieciešamās lomas</h3>
        <div className="checks">
          {roles.map((r) => (
            <label key={r} className="checkbox">
              <input
                type="checkbox"
                checked={selectedRoles.includes(r)}
                onChange={(e) =>
                  setSelectedRoles(e.target.checked ? [...selectedRoles, r] : selectedRoles.filter((x) => x !== r))
                }
              />
              <span>{r}</span>
            </label>
          ))}
        </div>

        <ErrorBox message={error} />
        <div className="actions top-gap">
          <button className="btn btn-primary" type="submit" disabled={saving}>
            {saving ? "Publicē…" : "Publicēt projektu"}
          </button>
          <button className="btn btn-ghost" type="button" onClick={() => navigate("/org/projects")}>
            Atcelt
          </button>
        </div>
      </form>
    </>
  );
}

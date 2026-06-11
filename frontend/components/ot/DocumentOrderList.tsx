"use client";

import {
  DndContext,
  KeyboardSensor,
  PointerSensor,
  closestCenter,
  type DragEndEvent,
  useSensor,
  useSensors,
} from "@dnd-kit/core";
import {
  SortableContext,
  arrayMove,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useRef } from "react";
import type { DocumentOrderItem } from "@/lib/ot/settings-api";
import { normalizeDocumentOrderItems } from "@/lib/ot/settings-api";
import { GradientButton } from "@/components/flit/Button";

type Props = {
  items: DocumentOrderItem[];
  onChange: (items: DocumentOrderItem[]) => void;
  onSave: () => void | Promise<void>;
  saving?: boolean;
  error?: string | null;
};

function SortableRow({
  item,
  onToggleIncluded,
}: {
  item: DocumentOrderItem;
  onToggleIncluded: (code: string, included: boolean) => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: item.document_type_code,
    disabled: !item.is_included,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <li
      ref={setNodeRef}
      style={style}
      data-testid={`doc-row-${item.document_type_code}`}
      className={`flex items-center gap-3 rounded-lg border px-3 py-2 ${
        item.is_included
          ? "border-flit-border bg-flit-surface"
          : "border-dashed border-flit-border/70 bg-flit-surface/50 opacity-70"
      } ${isDragging ? "shadow-md ring-2 ring-flit-brand/30" : ""}`}
    >
      <button
        type="button"
        aria-label={`Reordenar ${item.document_type_name}`}
        data-testid={`doc-handle-${item.document_type_code}`}
        className={`cursor-grab px-1 text-flit-text-secondary active:cursor-grabbing ${
          item.is_included ? "" : "invisible"
        }`}
        {...attributes}
        {...listeners}
      >
        ⋮⋮
      </button>

      <span className="min-w-8 text-xs font-semibold text-flit-text-secondary">{item.position}</span>

      <div className="flex-1">
        <p className="text-sm font-medium text-flit-text-primary">{item.document_type_name}</p>
        <p className="text-xs text-flit-text-secondary">{item.document_type_code}</p>
      </div>

      <label className="flex items-center gap-2 text-xs text-flit-text-primary">
        <input
          type="checkbox"
          checked={item.is_included}
          data-testid={`doc-include-${item.document_type_code}`}
          onChange={(event) => onToggleIncluded(item.document_type_code, event.target.checked)}
        />
        Incluir
      </label>
    </li>
  );
}

export function DocumentOrderList({ items, onChange, onSave, saving = false, error }: Props) {
  const itemsRef = useRef(items);
  itemsRef.current = items;

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  const includedItems = items
    .filter((item) => item.is_included)
    .toSorted((a, b) => a.position - b.position);
  const excludedItems = items.filter((item) => !item.is_included);
  const displayItems = [...includedItems, ...excludedItems];

  function onToggleIncluded(code: string, included: boolean) {
    const next = itemsRef.current.map((item) =>
      item.document_type_code === code ? { ...item, is_included: included } : item,
    );
    onChange(normalizeDocumentOrderItems(next));
  }

  function onDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over || active.id === over.id) {
      return;
    }

    const currentItems = itemsRef.current;
    const included = currentItems
      .filter((item) => item.is_included)
      .toSorted((a, b) => a.position - b.position);
    const oldIndex = included.findIndex((item) => item.document_type_code === active.id);
    const newIndex = included.findIndex((item) => item.document_type_code === over.id);
    if (oldIndex < 0 || newIndex < 0) {
      return;
    }

    const reordered = arrayMove(included, oldIndex, newIndex).map((item, index) => ({
      ...item,
      position: index + 1,
    }));
    const includedByCode = new Map(reordered.map((item) => [item.document_type_code, item]));
    const excluded = currentItems.filter((item) => !item.is_included);

    onChange([...reordered, ...excluded]);
  }

  return (
    <div className="space-y-4">
      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={onDragEnd}>
        <SortableContext
          items={includedItems.map((item) => item.document_type_code)}
          strategy={verticalListSortingStrategy}
        >
          <ul className="space-y-2" data-testid="document-order-list">
            {displayItems.map((item) => (
              <SortableRow key={item.document_type_code} item={item} onToggleIncluded={onToggleIncluded} />
            ))}
          </ul>
        </SortableContext>
      </DndContext>

      {error && (
        <p className="text-sm text-flit-danger" role="alert">
          {error}
        </p>
      )}

      <GradientButton type="button" disabled={saving} onClick={() => void onSave()}>
        {saving ? "Guardando…" : "Guardar orden de documentos"}
      </GradientButton>
    </div>
  );
}

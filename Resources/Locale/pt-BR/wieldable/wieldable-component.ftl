### Locale for wielding items; i.e. two-handing them

wieldable-verb-text-wield = Empunhar
wieldable-verb-text-unwield = Guardar

wieldable-component-successful-wield = Você empunha { THE($item) }.
wieldable-component-failed-wield = Você guarda { THE($item) }.
wieldable-component-successful-wield-other = { THE($user) } empunha { THE($item) }.
wieldable-component-failed-wield-other = { THE($user) } guarda { THE($item) }.

wieldable-component-no-hands = Você não tem mãos o suficiente!
wieldable-component-not-enough-free-hands = {$number -> 
    [one] Você precisa de uma mão livre para empunhar { THE($item) }.
    *[other] Você precisa de { $number } mãos livres para empunhar { THE($item) }.
}
wieldable-component-not-in-hands = { CAPITALIZE(THE($item)) } não está em suas mãos!
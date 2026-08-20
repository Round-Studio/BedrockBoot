import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'

const routes: Array<RouteRecordRaw> = [
    {
        path: '/',
        name: 'Home',
        component: () => import('../components/views/Home.vue')
    }
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

export default router
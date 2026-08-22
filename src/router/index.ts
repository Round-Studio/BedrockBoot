import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'

const routes: Array<RouteRecordRaw> = [
    {
        path: '/',
        name: 'Home',
        component: () => import('../components/views/Home.vue')
    },
    {
        path: '/download',
        name: 'Download',
        component: () => import('../components/views/Download.vue')
    },
    {
        path: '/privacyPolicy',
        name: 'PrivacyPolicy',
        component: () => import('../components/views/PrivacyPolicy.vue')
    },
    {
        path: '/404',
        name: 'NotFound',
        component: () => import('../components/views/NotFound.vue')
    },
    {
        path: '/:pathMatch(.*)*',
        name: 'CatchAll',
        redirect: '/404'
    }
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

export default router